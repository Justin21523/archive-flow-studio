using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Application.Nodes;
using ArchiveFlow.Application.Nodes.Query;
using ArchiveFlow.Application.Nodes.Actions;
using ArchiveFlow.Domain.Entities;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
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
    private readonly IWorkflowStorageService _workflowStorage; 
    private readonly IFilePreviewService _previewService;

    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ObservableCollection<EdgeViewModel> Edges { get; } = new();
    public ObservableCollection<FileRecord> ResultFiles { get; } = new();
    public ObservableCollection<MetadataValue> SelectedFileMetadata { get; } = new();

    [ObservableProperty] private string _executionLog = "Ready. Click buttons to add nodes.";
    [ObservableProperty] private string _tempEdgePath = string.Empty;
    [ObservableProperty] private bool _isConnecting;
    [ObservableProperty] private FileRecord? _selectedFile;
    [ObservableProperty] private Avalonia.Media.Imaging.Bitmap? _selectedFileThumbnail;
    [ObservableProperty] private string _selectedFilePreviewText = string.Empty;

    private PortViewModel? _connectingSourcePort;

    public NodeCanvasViewModel(
        IFileRepository fileRepository, 
        IMetadataRepository metadataRepository,
        ISearchService searchService,
        IWorkflowStorageService workflowStorage, 
        IFilePreviewService previewService,
        ILogger<NodeCanvasViewModel> logger)
    {
        _fileRepository = fileRepository;
        _metadataRepository = metadataRepository;
        _searchService = searchService;
        _previewService = previewService;
        _logger = logger;
        
        _logger.LogInformation("NodeCanvasViewModel constructor called");
        _workflowStorage = workflowStorage;
        InitializeDefaultNodes();
    }

    private void InitializeDefaultNodes()
    {
        _logger.LogInformation("Initializing default nodes...");
        
        Nodes.Clear();
        Edges.Clear();
        
        var allNode = new NodeViewModel("All Files", "AllFiles", 60, 160);
        var resultNode = new NodeViewModel("Result Table", "Result", 480, 160);
        
        Nodes.Add(allNode);
        Nodes.Add(resultNode);
        
        var edge = new EdgeViewModel(allNode.OutputPort, resultNode.InputPort);
        Edges.Add(edge);
        
        RecalculateLayout();
        ExecutionLog = $"Initialized with {Nodes.Count} nodes.";
        _logger.LogInformation($"Initialized with {Nodes.Count} nodes");
    }

    // --- Node Commands ---
    [RelayCommand] private void AddAllFiles() => AddNodeInternal("All Files", "AllFiles", 60, 100);
    [RelayCommand] private void AddFilterTxt() => AddNodeInternal("Filter: .txt", "FilterTxt", 300, 120);
    [RelayCommand] private void AddFilterMd() => AddNodeInternal("Filter: .md", "FilterMd", 300, 280);
    [RelayCommand] private void AddResultTable() => AddNodeInternal("Result Table", "Result", 520, 180);
    [RelayCommand] private void AddTagAI() => AddNodeInternal("Add Tag: AI", "AddTagAI", 300, 120);
    [RelayCommand] private void AddSubjectCS() => AddNodeInternal("Set Subject: CS", "SetSubjectCS", 300, 280);
    [RelayCommand] private void AddFullTextSearch() => AddNodeInternal("Full Text Search", "FullTextSearch", 300, 440, "test");
    [RelayCommand] private void AddExportCsv() => AddNodeInternal("Export CSV", "ExportCsv", 520, 120, "output.csv");
    [RelayCommand] private void AddExportJson() => AddNodeInternal("Export JSON", "ExportJson", 520, 300, "output.json");

    private void AddNodeInternal(string title, string type, double x, double y, string defaultParam = "")
    {
        double offsetX = (Nodes.Count % 5) * 30;
        var newNode = new NodeViewModel(title, type, x + offsetX, y + offsetX, defaultParam);
        Nodes.Add(newNode);
        RecalculateLayout();
        ExecutionLog += $"Added: {title}\n";
        _logger.LogInformation("Added node: {Title}", title);
    }

    // --- Connection & Drag ---
    public void StartConnection(PortViewModel sourcePort) 
    { 
        _connectingSourcePort = sourcePort; 
        IsConnecting = true; 
        TempEdgePath = string.Empty; 
    }
    
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
        if (_connectingSourcePort.IsInput || !targetPort.IsInput) { CancelConnection(); return; }
        if (_connectingSourcePort.ParentNode == targetPort.ParentNode) { CancelConnection(); return; }
        if (Edges.Any(e => e.Source == _connectingSourcePort && e.Target == targetPort)) { CancelConnection(); return; }
        var newEdge = new EdgeViewModel(_connectingSourcePort, targetPort);
        Edges.Add(newEdge); 
        newEdge.UpdateGeometry();
        ExecutionLog += "Connected nodes.\n";
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
    
    public void CancelConnection() 
    { 
        _connectingSourcePort = null; 
        IsConnecting = false; 
        TempEdgePath = string.Empty; 
    }
    
    public void UpdateNodePosition(NodeViewModel node, double newX, double newY) 
    { 
        node.X = Math.Max(0, newX); 
        node.Y = Math.Max(0, newY); 
        RecalculateLayout(); 
    }
    
    public void RecalculateLayout()
    {
        foreach (var node in Nodes) 
        {
            node.InputPort.AbsoluteX = node.X + node.InputPort.RelativeX; 
            node.InputPort.AbsoluteY = node.Y + node.InputPort.RelativeY;
            node.OutputPort.AbsoluteX = node.X + node.OutputPort.RelativeX; 
            node.OutputPort.AbsoluteY = node.Y + node.OutputPort.RelativeY;
        }
        foreach (var edge in Edges) edge.UpdateGeometry();
    }

    public async void SelectFile(FileRecord? file) 
    {
        SelectedFile = file; 
        SelectedFileMetadata.Clear();
        SelectedFileThumbnail = null;
        SelectedFilePreviewText = string.Empty;

        if (file == null)
        {
            return;
        }

        try
        {
            var metadata = await _metadataRepository.GetMetadataByFileIdAsync(file.Id);
            SelectedFileMetadata.Clear();
            foreach (var item in metadata)
            {
                SelectedFileMetadata.Add(item);
            }

            if (!string.IsNullOrWhiteSpace(file.ThumbnailPath) && File.Exists(file.ThumbnailPath))
            {
                await using var stream = File.OpenRead(file.ThumbnailPath);
                var bitmap = await Task.Run(() => new Bitmap(stream));
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SelectedFileThumbnail = bitmap;
                    SelectedFilePreviewText = string.Empty;
                });
            }
            else if (!string.IsNullOrWhiteSpace(file.ContentPreview))
            {
                SelectedFilePreviewText = file.ContentPreview;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load preview for file {FileId}", file.Id);
            ExecutionLog += $"Preview load error: {ex.Message}\n";
        }
    }

    // --- Execution ---
    [RelayCommand]
    private async Task ExecuteWorkflowAsync()
    {
        ExecutionLog = "Executing workflow...\n";
        ResultFiles.Clear();
        SelectedFileMetadata.Clear();
        SelectedFile = null;

        try
        {
            var sortedNodes = GetTopologicalOrder();
            _logger.LogInformation($"Executing {sortedNodes.Count} nodes");
            
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
            ExecutionLog += $"\nWorkflow completed. Total: {ResultFiles.Count} files\n";
        }
        catch (Exception ex) 
        { 
            _logger.LogError(ex, "Workflow failed");
            ExecutionLog += $"Error: {ex.Message}\n"; 
        }
    }

    private List<NodeViewModel> GetTopologicalOrder()
    {
        var inDegree = new Dictionary<Guid, int>(); 
        var adj = new Dictionary<Guid, List<Guid>>();
        foreach (var node in Nodes) { inDegree[node.Id] = 0; adj[node.Id] = new List<Guid>(); }
        foreach (var edge in Edges) { adj[edge.Source.ParentNode.Id].Add(edge.Target.ParentNode.Id); inDegree[edge.Target.ParentNode.Id]++; }
        var queue = new Queue<Guid>();
        foreach (var kvp in inDegree) if (kvp.Value == 0) queue.Enqueue(kvp.Key);
        var sortedIds = new List<Guid>();
        while (queue.Count > 0) { var curr = queue.Dequeue(); sortedIds.Add(curr); foreach (var n in adj[curr]) { inDegree[n]--; if (inDegree[n] == 0) queue.Enqueue(n); } }
        return Nodes.Where(n => sortedIds.Contains(n.Id)).OrderBy(n => sortedIds.IndexOf(n.Id)).ToList();
    }

    private IArchiveNode CreateBackendNode(NodeViewModel vm)
    {
        return vm.NodeType switch
        {
            "AllFiles" => new AllFilesNode(_fileRepository, _searchService, _previewService), 
            "FilterTxt" => new FileTypeFilterNode(".txt"),
            "FilterMd" => new FileTypeFilterNode(".md"),
            "Result" => new PassThroughNode(),
            "AddTagAI" => new AddTagNode(_metadataRepository, "AI"),
            "SetSubjectCS" => new SetSubjectNode(_metadataRepository, "Computer Science"),
            "FullTextSearch" => new FullTextSearchNode(_searchService, vm.ParameterValue),
            "ExportCsv" => new ExportCsvNode(vm.ParameterValue),
            "ExportJson" => new ExportJsonNode(vm.ParameterValue),
            _ => throw new InvalidOperationException($"Unknown: {vm.NodeType}")
        };
    }

    // --- 新增：Save/Load Workflow ---

    [RelayCommand]
    private async Task SaveWorkflowAsync()
    {
        try
        {
            var dto = new ArchiveFlow.Application.DTOs.WorkflowDto
            {
                Name = "MyWorkflow", // 未來可以改成讓使用者輸入
                Nodes = Nodes.Select(n => new ArchiveFlow.Application.DTOs.NodeDto
                {
                    Id = n.Id.ToString(),
                    Type = n.NodeType,
                    Title = n.Title,
                    X = n.X,
                    Y = n.Y,
                    Parameter = n.ParameterValue
                }).ToList(),
                Connections = Edges.Select(e => new ArchiveFlow.Application.DTOs.ConnectionDto
                {
                    SourceNodeId = e.Source.ParentNode.Id.ToString(),
                    TargetNodeId = e.Target.ParentNode.Id.ToString()
                }).ToList()
            };

            await _workflowStorage.SaveWorkflowAsync("current_workflow", dto);
            ExecutionLog += "Workflow saved successfully.\n";
        }
        catch (Exception ex)
        {
            ExecutionLog += $"Error saving workflow: {ex.Message}\n";
        }
    }

    [RelayCommand]
    private async Task LoadWorkflowAsync()
    {
        try
        {
            var dto = await _workflowStorage.LoadWorkflowAsync("current_workflow");
            if (dto == null)
            {
                ExecutionLog += "No saved workflow found.\n";
                return;
            }

            // 清空現有狀態
            Nodes.Clear();
            Edges.Clear();
            
            // 重建節點
            var nodeMap = new Dictionary<string, NodeViewModel>();
            foreach (var nodeDto in dto.Nodes)
            {
                var id = Guid.Parse(nodeDto.Id);
                var node = new NodeViewModel(id, nodeDto.Title, nodeDto.Type, nodeDto.X, nodeDto.Y, nodeDto.Parameter);
                Nodes.Add(node);
                nodeMap[nodeDto.Id] = node;
            }
            // 重建連線
            foreach (var connDto in dto.Connections)
            {
                if (nodeMap.TryGetValue(connDto.SourceNodeId, out var sourceNode) &&
                    nodeMap.TryGetValue(connDto.TargetNodeId, out var targetNode))
                {
                    var edge = new EdgeViewModel(sourceNode.OutputPort, targetNode.InputPort);
                    Edges.Add(edge);
                }
            }

            RecalculateLayout();
            ExecutionLog += $"Workflow loaded. {Nodes.Count} nodes, {Edges.Count} connections.\n";
        }
        catch (Exception ex)
        {
            ExecutionLog += $"Error loading workflow: {ex.Message}\n";
        }
    }
}

public class PassThroughNode : IArchiveNode
{
    public Guid Id { get; } = Guid.NewGuid(); public string DisplayName => "Result"; public double X { get; set; } public double Y { get; set; }
    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
