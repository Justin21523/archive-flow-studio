using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Application.Nodes;
using ArchiveFlow.Application.Nodes.Query;
using ArchiveFlow.Application.Nodes.Actions;
using ArchiveFlow.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.App.ViewModels;

public partial class NodeCanvasViewModel : ObservableObject
{
    private readonly IFileRepository _fileRepository;
    private readonly IMetadataRepository _metadataRepository;
    private readonly ISearchService _searchService; 
    private readonly ILogger<NodeCanvasViewModel> _logger;

    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ObservableCollection<EdgeViewModel> Edges { get; } = new();
    public ObservableCollection<FileRecord> ResultFiles { get; } = new();
    public ObservableCollection<MetadataValue> SelectedFileMetadata { get; } = new();

    [ObservableProperty]
    private string _executionLog = "Ready.";

    [ObservableProperty]
    private string _tempEdgePath = string.Empty;

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private FileRecord? _selectedFile;

    private PortViewModel? _connectingSourcePort;

    public NodeCanvasViewModel(
        IFileRepository fileRepository, 
        IMetadataRepository metadataRepository,
        ISearchService searchService,
        ILogger<NodeCanvasViewModel> logger)
    {
        _fileRepository = fileRepository;
        _metadataRepository = metadataRepository;
        _searchService = searchService;
        _logger = logger;
        InitializeDefaultNodes();
    }

    private void InitializeDefaultNodes()
    {
        var allNode = new NodeViewModel("All Files", "AllFiles", 100, 150);
        var resultNode = new NodeViewModel("Result Table", "Result", 600, 150);
        
        Nodes.Add(allNode);
        Nodes.Add(resultNode);
        
        var edge = new EdgeViewModel(allNode.OutputPort, resultNode.InputPort);
        Edges.Add(edge);
        
        RecalculateLayout();
    }

    // --- Node Commands ---
    [RelayCommand] private void AddAllFiles() => AddNodeInternal("All Files", "AllFiles", 120, 120);
    [RelayCommand] private void AddFilterTxt() => AddNodeInternal("Filter: .txt", "FilterTxt", 340, 120);
    [RelayCommand] private void AddFilterMd() => AddNodeInternal("Filter: .md", "FilterMd", 340, 260);
    [RelayCommand] private void AddResultTable() => AddNodeInternal("Result Table", "Result", 620, 180);
    
    // Metadata Nodes
    [RelayCommand] private void AddTagAI() => AddNodeInternal("Add Tag: AI", "AddTagAI", 420, 120);
    [RelayCommand] private void AddSubjectCS() => AddNodeInternal("Set Subject: CS", "SetSubjectCS", 420, 240);
    
    // 新增：全文搜尋節點
    [RelayCommand] private void AddFullTextSearch() => AddNodeInternal("Full Text Search", "FullTextSearch", 340, 400, "test");

    private void AddNodeInternal(string title, string type, double x, double y, string defaultParam = "")
    {
        var offset = (Nodes.Count % 7) * 28;
        var newNode = new NodeViewModel(title, type, x + offset, y + offset, defaultParam);
        Nodes.Add(newNode);
        RecalculateLayout();
        ExecutionLog += $"Added: {title}\n";
    }

    // --- Connection & Drag ---
    public void StartConnection(PortViewModel sourcePort) { _connectingSourcePort = sourcePort; IsConnecting = true; TempEdgePath = string.Empty; }
    public void UpdateTempConnection(double currentX, double currentY)
    {
        if (!IsConnecting || _connectingSourcePort == null) return;
        double x1 = _connectingSourcePort.AbsoluteX, y1 = _connectingSourcePort.AbsoluteY;
        double dx = Math.Abs(currentX - x1) * 0.5;
        TempEdgePath = $"M {x1},{y1} C {x1 + dx},{y1} {currentX - dx},{currentY} {currentX},{currentY}";
    }
    public void FinishConnection(PortViewModel targetPort)
    {
        if (_connectingSourcePort == null || targetPort == _connectingSourcePort) { CancelConnection(); return; }
        if (!targetPort.IsInput || _connectingSourcePort.IsInput) { CancelConnection(); return; }
        if (targetPort.ParentNode == _connectingSourcePort.ParentNode) { CancelConnection(); return; }
        if (Edges.Any(e => e.Source == _connectingSourcePort && e.Target == targetPort)) { CancelConnection(); return; }
        var newEdge = new EdgeViewModel(_connectingSourcePort, targetPort);
        Edges.Add(newEdge); newEdge.UpdateGeometry();
        CancelConnection();
    }
    public bool TryFinishConnectionAt(double x, double y)
    {
        if (_connectingSourcePort == null) return false;

        var targetPort = Nodes
            .Select(node => node.InputPort)
            .Where(port => port.ParentNode != _connectingSourcePort.ParentNode)
            .Select(port => new
            {
                Port = port,
                DistanceSquared = Math.Pow(port.AbsoluteX - x, 2) + Math.Pow(port.AbsoluteY - y, 2)
            })
            .Where(candidate => candidate.DistanceSquared <= 24 * 24)
            .OrderBy(candidate => candidate.DistanceSquared)
            .Select(candidate => candidate.Port)
            .FirstOrDefault();

        if (targetPort == null) return false;

        FinishConnection(targetPort);
        return true;
    }
    public void CancelConnection() { _connectingSourcePort = null; IsConnecting = false; TempEdgePath = string.Empty; }
    public void UpdateNodePosition(NodeViewModel node, double newX, double newY) { node.X = Math.Max(0, newX); node.Y = Math.Max(0, newY); RecalculateLayout(); }
    public void RecalculateLayout()
    {
        foreach (var node in Nodes) {
            node.InputPort.AbsoluteX = node.X + node.InputPort.RelativeX; node.InputPort.AbsoluteY = node.Y + node.InputPort.RelativeY;
            node.OutputPort.AbsoluteX = node.X + node.OutputPort.RelativeX; node.OutputPort.AbsoluteY = node.Y + node.OutputPort.RelativeY;
        }
        foreach (var edge in Edges) edge.UpdateGeometry();
    }

    // --- Selection & Inspector ---
    public void SelectFile(FileRecord? file)
    {
        SelectedFile = file;
        SelectedFileMetadata.Clear();
        if (file != null)
        {
            Task.Run(async () => {
                var metadata = await _metadataRepository.GetMetadataByFileIdAsync(file.Id);
                foreach (var m in metadata) SelectedFileMetadata.Add(m);
            });
        }
    }

    // --- Execution ---
    [RelayCommand]
    private async Task ExecuteWorkflowAsync()
    {
        ExecutionLog = "Executing...\n";
        ResultFiles.Clear();
        SelectedFileMetadata.Clear();
        SelectedFile = null;

        try
        {
            var sortedNodes = GetTopologicalOrder();
            var context = new NodeExecutionContext();

            foreach (var nodeVm in sortedNodes)
            {
                nodeVm.Status = "Running";
                var backendNode = CreateBackendNode(nodeVm);
                await backendNode.ExecuteAsync(context, CancellationToken.None);
                nodeVm.Status = $"Success ({context.CurrentFileSet.Count})";
                ExecutionLog += $"[{nodeVm.Title}] Count: {context.CurrentFileSet.Count}\n";
            }

            foreach (var file in context.CurrentFileSet) ResultFiles.Add(file);
            ExecutionLog += $"Done. Total: {ResultFiles.Count}\n";
        }
        catch (Exception ex) { ExecutionLog += $"Error: {ex.Message}\n"; }
    }

    private List<NodeViewModel> GetTopologicalOrder()
    {
        var inDegree = new Dictionary<Guid, int>(); var adj = new Dictionary<Guid, List<Guid>>();
        foreach (var node in Nodes) { inDegree[node.Id] = 0; adj[node.Id] = new List<Guid>(); }
        foreach (var edge in Edges) { adj[edge.Source.ParentNode.Id].Add(edge.Target.ParentNode.Id); inDegree[edge.Target.ParentNode.Id]++; }
        var queue = new Queue<Guid>();
        foreach (var kvp in inDegree) if (kvp.Value == 0) queue.Enqueue(kvp.Key);
        var sortedIds = new List<Guid>();
        while (queue.Count > 0) {
            var curr = queue.Dequeue(); sortedIds.Add(curr);
            foreach (var neighbor in adj[curr]) { inDegree[neighbor]--; if (inDegree[neighbor] == 0) queue.Enqueue(neighbor); }
        }
        return Nodes.Where(n => sortedIds.Contains(n.Id)).OrderBy(n => sortedIds.IndexOf(n.Id)).ToList();
    }

    private IArchiveNode CreateBackendNode(NodeViewModel vm)
    {
        return vm.NodeType switch
        {
            "AllFiles" => new AllFilesNode(_fileRepository, _searchService),
            "FilterTxt" => new FileTypeFilterNode(".txt"),
            "FilterMd" => new FileTypeFilterNode(".md"),
            "FullTextSearch" => new FullTextSearchNode(_searchService, vm.ParameterValue),
            "Result" => new PassThroughNode(),
            "AddTagAI" => new AddTagNode(_metadataRepository, "AI"),
            "SetSubjectCS" => new SetSubjectNode(_metadataRepository, "Computer Science"),
            _ => throw new InvalidOperationException($"Unknown: {vm.NodeType}")
        };
    }
}

public class PassThroughNode : IArchiveNode
{
    public Guid Id { get; } = Guid.NewGuid(); public string DisplayName => "Result"; public double X { get; set; } public double Y { get; set; }
    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
