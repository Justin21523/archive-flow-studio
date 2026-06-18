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
        ILogger<NodeCanvasViewModel> logger)
    {
        _fileRepository = fileRepository;
        _metadataRepository = metadataRepository;
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
    [RelayCommand] private void AddAllFiles() => AddNodeInternal("All Files", "AllFiles", 100, 100);
    [RelayCommand] private void AddFilterTxt() => AddNodeInternal("Filter: .txt", "FilterTxt", 300, 100);
    [RelayCommand] private void AddFilterMd() => AddNodeInternal("Filter: .md", "FilterMd", 300, 250);
    [RelayCommand] private void AddResultTable() => AddNodeInternal("Result Table", "Result", 600, 150);
    
    // Metadata Nodes
    [RelayCommand] private void AddTagAI() => AddNodeInternal("Add Tag: AI", "AddTagAI", 400, 100);
    [RelayCommand] private void AddSubjectCS() => AddNodeInternal("Set Subject: CS", "SetSubjectCS", 400, 200);

    private void AddNodeInternal(string title, string type, double x, double y)
    {
        double offsetX = (Nodes.Count % 5) * 30;
        var newNode = new NodeViewModel(title, type, x + offsetX, y);
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
        if (Edges.Any(e => e.Source == _connectingSourcePort && e.Target == targetPort)) { CancelConnection(); return; }
        var newEdge = new EdgeViewModel(_connectingSourcePort, targetPort);
        Edges.Add(newEdge); newEdge.UpdateGeometry();
        CancelConnection();
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
            "AllFiles" => new AllFilesNode(_fileRepository),
            "FilterTxt" => new FileTypeFilterNode(".txt"),
            "FilterMd" => new FileTypeFilterNode(".md"),
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