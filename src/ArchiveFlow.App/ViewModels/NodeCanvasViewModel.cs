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
using ArchiveFlow.Application.DTOs;
using ArchiveFlow.Application.Services;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
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
    private readonly IAutoTaggingService _autoTaggingService;
    private readonly IBatchJobService _batchJobService;
    private readonly NodeRegistry _nodeRegistry;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IDublinCoreExportService _dcExportService;
    public ObservableCollection<NodeLibraryItem> NodeLibraryTree { get; } = new();

    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ObservableCollection<EdgeViewModel> Edges { get; } = new();
    public ObservableCollection<FileRecord> ResultFiles { get; } = new();
    public ObservableCollection<MetadataValue> SelectedFileMetadata { get; } = new();
    public ObservableCollection<NodeDefinition> PluginNodeDefinitions { get; } = new();

    [ObservableProperty] private string _executionLog = "Ready. Click buttons to add nodes.";
    [ObservableProperty] private string _tempEdgePath = string.Empty;
    [ObservableProperty] private bool _isConnecting;
    [ObservableProperty] private FileRecord? _selectedFile;
    [ObservableProperty] private Avalonia.Media.Imaging.Bitmap? _selectedFileThumbnail;
    [ObservableProperty] private string _selectedFilePreviewText = string.Empty;
    [ObservableProperty] private NodeViewModel? _selectedNode;
    [ObservableProperty] private double _canvasViewportCenterX = 1500;
    [ObservableProperty] private double _canvasViewportCenterY = 1000;
    [ObservableProperty] private BatchJobInfo _currentBatchJob = new() { JobName = "Idle", StatusMessage = "No active jobs." };

    private const double NodeDefaultWidth = 200;
    private const double NodeDefaultHeight = 130;
    private PortViewModel? _connectingSourcePort;

    public NodeCanvasViewModel(
        IFileRepository fileRepository, 
        IMetadataRepository metadataRepository,
        ISearchService searchService,
        IWorkflowStorageService workflowStorage,
        IFilePreviewService previewService,
        IAutoTaggingService autoTaggingService,
        IBatchJobService batchJobService,
        NodeRegistry nodeRegistry,
        IRelationshipRepository relationshipRepository,
        IDublinCoreExportService dcExportService,
        ILogger<NodeCanvasViewModel> logger)
    {
        _fileRepository = fileRepository;
        _metadataRepository = metadataRepository;
        _searchService = searchService;
        _workflowStorage = workflowStorage;
        _previewService = previewService;
        _autoTaggingService = autoTaggingService;
        _batchJobService = batchJobService;
        _nodeRegistry = nodeRegistry;
        _relationshipRepository = relationshipRepository;
        _dcExportService = dcExportService;
        _logger = logger;
        
        InitializeDefaultNodes();
        InitializeNodeLibraryTree(); 
        SubscribeToBatchJobEvents();
    }

    // 載入插件節點到 UI 集合
    private void LoadPluginNodes()
    {
        var pluginNodes = _nodeRegistry.GetDefinitionsByCategory("Plugins");
        foreach (var def in pluginNodes)
        {
            PluginNodeDefinitions.Add(def);
        }
    }
    // 新增 Command 用於動態新增插件節點
    [RelayCommand]
    private void AddPluginNode(NodeDefinition definition)
    {
        if (definition == null) return;
        AddNodeInternal(definition.DisplayName, definition.NodeType);
    }

    [RelayCommand]
    private async Task CopyFileIdAsync()
    {
        if (SelectedFile == null) return;

        // 複製 ID 到剪貼簿
        var clipboard = (global::Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow?.Clipboard;
        
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(SelectedFile.Id);
            
            // 顯示通知（可選）
            ExecutionLog += $"Copied File ID to clipboard: {SelectedFile.Id}\n";
        }
    }    
    
    // Add these commands for performance optimization
    [RelayCommand]
    private void ClearCanvas()
    {
        Nodes.Clear();
        Edges.Clear();
        SelectedNode = null;
    }
    
    [RelayCommand]
    private void DuplicateNode(NodeViewModel? node)
    {
        if (node == null) return;
        
        var newNode = new NodeViewModel(
            $"{node.Title} (Copy)",
            node.NodeType,
            node.X + 30,
            node.Y + 30,
            node.ParameterValue);
        
        // Copy parameters
        foreach (var param in node.Parameters)
        {
            newNode.Parameters.Add(new NodeParameterViewModel(
                param.Label, param.Type, param.Value));
        }
        
        Nodes.Add(newNode);
        SelectNode(newNode);
    }
    // Performance optimization: Lazy loading for large datasets
    private bool _isVirtualized = true;
    
    public bool IsVirtualized
    {
        get => _isVirtualized;
        set
        {
            _isVirtualized = value;
            // Trigger UI refresh
        }
    }

    // 4. 新增訂閱背景任務事件的方法
    private void SubscribeToBatchJobEvents()
    {
        _batchJobService.OnJobStarted += (name) => 
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => 
            {
                CurrentBatchJob = new BatchJobInfo 
                { 
                    JobName = name, 
                    StatusMessage = "Starting...", 
                    IsActive = true 
                };
            });
        };

        _batchJobService.OnJobProgress += (name, msg) => 
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => 
            {
                CurrentBatchJob = new BatchJobInfo
                {
                    JobName = name,
                    StatusMessage = msg,
                    IsActive = true
                };
            });
        };

        _batchJobService.OnJobCompleted += (name) => 
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => 
            {
                CurrentBatchJob = new BatchJobInfo
                {
                    JobName = name,
                    StatusMessage = "Completed.",
                    IsActive = false
                };
            });
        };
    }
    
    // 5. 新增一個測試用的 Command 來模擬背景任務
    [RelayCommand]
    private void RunMockBatchJob()
    {
        _batchJobService.EnqueueJob("Simulated Thumbnail Generation", async (ct, progress) =>
        {
            for (int i = 1; i <= 10; i++)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(500, ct); // 模擬耗時操作
                progress.Report($"Processing file {i}/10...");
            }
            progress.Report("Completed!");
        });
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
    // Sources
    [RelayCommand] private void AddAllFiles() => AddNodeInternal("All Files", "AllFiles");
    [RelayCommand] private void AddFolderScanner() => AddNodeInternal("Folder Scanner", "FolderScanner");

    // Filters
    [RelayCommand] private void AddFilterTxt() => AddNodeInternal("Filter: .txt", "FilterTxt");
    [RelayCommand] private void AddFilterMd() => AddNodeInternal("Filter: .md", "FilterMd");
    [RelayCommand] private void AddDynamicRule() => AddNodeInternal("Dynamic Rule", "DynamicRule", "type:.png");
    [RelayCommand] private void AddFullTextSearch() => AddNodeInternal("Full Text Search", "FullTextSearch", "search term");

    // Actions
    [RelayCommand] private void AddTagAI() => AddNodeInternal("Add Tag: AI", "AddTagAI");
    [RelayCommand] private void AddSubjectCS() => AddNodeInternal("Set Subject: CS", "SetSubjectCS");
    [RelayCommand] private void AddAutoTag() => AddNodeInternal("Auto-Tag Files", "AutoTag");

    // DAG Logic
    [RelayCommand] private void AddConditionBranch() => AddNodeInternal("Condition Branch", "ConditionBranch", "size:>5000");
    [RelayCommand] private void AddMergeBranches() => AddNodeInternal("Merge Branches", "MergeBranches");

    // Relationships
    [RelayCommand] private void AddCreateRelationship() => AddNodeInternal("Link to Target", "CreateRelationship", "TARGET_FILE_ID_HERE");
    [RelayCommand] private void AddFindRelated() => AddNodeInternal("Find Related", "FindRelated", "SOURCE_FILE_ID_HERE");

    // Outputs
    [RelayCommand] private void AddResultTable() => AddNodeInternal("Result Table", "Result");
    [RelayCommand] private void AddExportCsv() => AddNodeInternal("Export CSV", "ExportCsv", "output.csv");
    [RelayCommand] private void AddExportJson() => AddNodeInternal("Export JSON", "ExportJson", "output.json");
    [RelayCommand] private void AddExportDublinCore() => AddNodeInternal("Export Dublin Core", "ExportDcXml", "metadata_export.xml");
    
    // --- Update the CreateBackendNode method to include new cases ---
    private IArchiveNode CreateBackendNode(NodeViewModel vm)
    {
        // 1. Check built-in nodes first
        var builtInNode = vm.NodeType switch
        {
            "AllFiles" => new AllFilesNode(_fileRepository, _searchService, _previewService),
            "FolderScanner" => new FolderScannerNode(_fileRepository, _searchService, _previewService),
            "FilterTxt" => new FileTypeFilterNode(".txt"),
            "FilterMd" => new FileTypeFilterNode(".md"),
            "FullTextSearch" => new FullTextSearchNode(_searchService, vm.ParameterValue),
            "DynamicRule" => new DynamicRuleNode(vm.ParameterValue),
            "AddTagAI" => new AddTagNode(_metadataRepository, "AI"),
            "SetSubjectCS" => new SetSubjectNode(_metadataRepository, "Computer Science"),
            "AutoTag" => new AutoTagNode(_autoTaggingService),
            "ConditionBranch" => new ConditionBranchNode(vm.ParameterValue),
            "MergeBranches" => new MergeBranchesNode(),
            "CreateRelationship" => new CreateRelationshipNode(_relationshipRepository, vm.ParameterValue, "References"),
            "FindRelated" => new FindRelatedFilesNode(_relationshipRepository, _fileRepository, vm.ParameterValue),
            "Result" => new PassThroughNode(),
            "ExportCsv" => new ExportCsvNode(vm.ParameterValue),
            "ExportJson" => new ExportJsonNode(vm.ParameterValue),
            "ExportDcXml" => new ExportDublinCoreNode(_dcExportService, vm.ParameterValue),
            _ => (IArchiveNode?)null
        };

        if (builtInNode != null) return builtInNode;

        // 2. Fallback to Plugin Registry for dynamically loaded nodes
        if (_nodeRegistry != null)
        {
            var definition = _nodeRegistry.GetDefinition(vm.NodeType);
            if (definition != null)
            {
                // Create instance using the plugin's factory method
                var pluginNode = definition.Factory();
                
                // Set position and display name to match the UI node
                pluginNode.X = vm.X;
                pluginNode.Y = vm.Y;
                // Note: DisplayName is usually read-only in the interface, but if your implementation allows setting, do it here.
                
                return pluginNode;
            }
        }

        // 3. If still not found, throw an exception (or return a dummy error node)
        _logger.LogWarning("Unknown node type encountered during execution: {NodeType}", vm.NodeType);
        throw new InvalidOperationException($"Unknown or unregistered node type: {vm.NodeType}");
    }

    public void UpdateCanvasViewportCenter(double centerX, double centerY)
    {
        CanvasViewportCenterX = Math.Max(NodeDefaultWidth / 2, centerX);
        CanvasViewportCenterY = Math.Max(NodeDefaultHeight / 2, centerY);
    }

    private void AddNodeInternal(string title, string type, string defaultParam = "")
    {
        var x = Math.Max(0, CanvasViewportCenterX - NodeDefaultWidth / 2);
        var y = Math.Max(0, CanvasViewportCenterY - NodeDefaultHeight / 2);
        var newNode = new NodeViewModel(title, type, x, y, defaultParam);

        newNode.AddTextParam("General Parameter", defaultParam);

        // Initialize specific parameters based on node type
        switch (type)
        {
            case "FilterTxt":
            case "FilterMd":
                newNode.AddDropdownParam("Extension", ".txt", ".md", ".csv", ".json");
                break;
            case "FullTextSearch":
                newNode.AddTextParam("Keyword", "search term");
                newNode.AddDropdownParam("Mode", "Exact", "Fuzzy", "Regex");
                break;
            case "ConditionBranch":
                newNode.AddDropdownParam("Field", "size", "extension", "name", "status");
                newNode.AddDropdownParam("Operator", ">", "<", "==", "contains");
                newNode.AddTextParam("Value", "0");
                break;
            case "ExportCsv":
            case "ExportJson":
                newNode.AddTextParam("Filename", "output");
                newNode.AddDropdownParam("Encoding", "UTF-8", "ASCII");
                break;
            case "CreateRelationship":
                newNode.AddTextParam("Target File ID", defaultParam);
                newNode.AddDropdownParam("Relation Type", "References", "HasNote", "UsesAsset", "RelatedTo");
                break;
            case "FindRelated":
                newNode.AddTextParam("Source File ID", defaultParam);
                break;
            case "ExportDcXml":
                newNode.AddTextParam("Output XML", string.IsNullOrWhiteSpace(defaultParam) ? "metadata_export.xml" : defaultParam);
                break;
        }

        Nodes.Add(newNode);
        SelectNode(newNode);
        RecalculateLayout();
        ExecutionLog += $"Added at canvas center: {title}\n";
        _logger.LogInformation("Added node at canvas center: {Title}", title);
    }

    public void SelectNode(NodeViewModel? node)
    {
        // Deselect previous
        if (SelectedNode != null) SelectedNode.IsSelected = false;
        
        SelectedNode = node;
        if (SelectedNode != null) SelectedNode.IsSelected = true;
    }

    [RelayCommand]
    private void DeleteSelectedNode()
    {
        if (SelectedNode == null) return;
        var deletedTitle = SelectedNode.Title;

        // Remove edges connected to this node
        var edgesToRemove = Edges.Where(e => e.Source.ParentNode == SelectedNode || e.Target.ParentNode == SelectedNode).ToList();
        foreach (var edge in edgesToRemove)
        {
            Edges.Remove(edge);
        }

        // Remove node
        Nodes.Remove(SelectedNode);
        SelectNode(null);
        
        ExecutionLog += $"Deleted node: {deletedTitle}\n";
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

    private void InitializeNodeLibraryTree()
    {
        NodeLibraryTree.Clear();

        // 1. Sources
        var sources = new NodeLibraryItem("Sources", isCategory: true);
        sources.Children.Add(new NodeLibraryItem("All Files", "AllFiles"));
        sources.Children.Add(new NodeLibraryItem("Folder Scanner", "FolderScanner")); // Placeholder
        NodeLibraryTree.Add(sources);

        // 2. Processors (Query/Filter)
        var processors = new NodeLibraryItem("Processors", isCategory: true);
        var filters = new NodeLibraryItem("Filters", isCategory: true);
        filters.Children.Add(new NodeLibraryItem("File Type (.txt)", "FilterTxt"));
        filters.Children.Add(new NodeLibraryItem("File Type (.md)", "FilterMd"));
        filters.Children.Add(new NodeLibraryItem("Dynamic Rule", "DynamicRule"));
        processors.Children.Add(filters);

        var search = new NodeLibraryItem("Search", isCategory: true);
        search.Children.Add(new NodeLibraryItem("Full Text Search", "FullTextSearch"));
        processors.Children.Add(search);

        var dag = new NodeLibraryItem("DAG Logic", isCategory: true);
        dag.Children.Add(new NodeLibraryItem("Condition Branch", "ConditionBranch"));
        dag.Children.Add(new NodeLibraryItem("Merge Branches", "MergeBranches"));
        processors.Children.Add(dag);
        NodeLibraryTree.Add(processors);
        // 3. Actions (Metadata/Modify)
        var actions = new NodeLibraryItem("Actions", isCategory: true);
        var metadata = new NodeLibraryItem("Metadata", isCategory: true);
        metadata.Children.Add(new NodeLibraryItem("Add Tag: AI", "AddTagAI"));
        metadata.Children.Add(new NodeLibraryItem("Set Subject: CS", "SetSubjectCS"));
        metadata.Children.Add(new NodeLibraryItem("Auto-Tag (AI)", "AutoTag"));
        actions.Children.Add(metadata);
        NodeLibraryTree.Add(actions);

        // 4. Outputs
        var outputs = new NodeLibraryItem("Outputs", isCategory: true);
        outputs.Children.Add(new NodeLibraryItem("Export Dublin Core XML", "ExportDcXml"));
        outputs.Children.Add(new NodeLibraryItem("Result Table", "Result"));
        outputs.Children.Add(new NodeLibraryItem("Export CSV", "ExportCsv"));
        outputs.Children.Add(new NodeLibraryItem("Export JSON", "ExportJson"));
        NodeLibraryTree.Add(outputs);

        // Relationship
        var relationships = new NodeLibraryItem("Relationships", isCategory: true);
        relationships.Children.Add(new NodeLibraryItem("Link to Target", "CreateRelationship"));
        relationships.Children.Add(new NodeLibraryItem("Find Related", "FindRelated"));
        NodeLibraryTree.Add(relationships);
    }

}

public class PassThroughNode : IArchiveNode
{
    public Guid Id { get; } = Guid.NewGuid(); public string DisplayName => "Result"; public double X { get; set; } public double Y { get; set; }
    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
