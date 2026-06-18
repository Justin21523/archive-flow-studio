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
using ArchiveFlow.Application.Nodes.Definitions;
using ArchiveFlow.Domain.Entities;
using ArchiveFlow.Application.DTOs;
using ArchiveFlow.Application.Services;
using ArchiveFlow.App.Views;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodeDefinition = ArchiveFlow.Application.Nodes.Definitions.NodeDefinition;
using NodeRegistry = ArchiveFlow.Application.Services.NodeRegistry;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// Represents a grouped category in the Node Library TreeView.
/// </summary>
public class NodeCategoryGroup
{
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<NodeDefinition> Definitions { get; set; } = new();
}

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
    private readonly IServiceProvider _serviceProvider;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IDublinCoreExportService _dcExportService;
    public ObservableCollection<NodeLibraryItem> NodeLibraryTree { get; } = new();
    public ObservableCollection<NodeCategoryGroup> NodeLibraryGroups { get; } = new();
    public event Action? NodesChanged;

    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ObservableCollection<EdgeViewModel> Edges { get; } = new();
    public ObservableCollection<FileRecord> ResultFiles { get; } = new();
    public ObservableCollection<MetadataValue> SelectedFileMetadata { get; } = new();
    public ObservableCollection<MetadataGroup> GroupedMetadata { get; } = new();
    public ObservableCollection<NodeDefinition> PluginNodeDefinitions { get; } = new();
    public ObservableCollection<FileRelationship> SelectedFileRelationships { get; } = new();

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
    [ObservableProperty] private ObservableCollection<ActionPreview> _pendingPreviews = new();
    [ObservableProperty] private bool _hasPendingActions = false;   

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
        IServiceProvider serviceProvider,
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
        _serviceProvider = serviceProvider;
        _relationshipRepository = relationshipRepository;
        _dcExportService = dcExportService;
        _logger = logger;
        
        InitializeDefaultNodes();
        InitializeNodeLibraryTree(); 
        SubscribeToBatchJobEvents();
        InitializeNodeLibraryFromRegistry();
    }
    
    // Phase 4: Query / Filter / Search Nodes
    [RelayCommand] private void AddDateRangeFilter() => AddNodeInternal("Date Range Filter", "DateRangeFilter", 300, 350, "imported:>2024-01-01");
    [RelayCommand] private void AddSizeFilter() => AddNodeInternal("Size Filter", "SizeFilter", 300, 400, ">5MB");
    [RelayCommand] private void AddTagFilter() => AddNodeInternal("Tag Filter", "TagFilter", 300, 450, "ai");
    [RelayCommand] private void AddSort() => AddNodeInternal("Sort", "Sort", 500, 100, "size:desc");
    [RelayCommand] private void AddLimit() => AddNodeInternal("Limit", "Limit", 500, 150, "100");
    [RelayCommand] private void AddRegexSearch() => AddNodeInternal("Regex Search", "RegexSearch", 300, 500, "name:^report_.*");
    
    /// <summary>
    /// Dynamically builds the hierarchical Node Library from the NodeRegistry.
    /// </summary>
    private void InitializeNodeLibraryFromRegistry()
    {
        NodeLibraryGroups.Clear();
        
        var grouped = _nodeRegistry.GetAllDefinitions()
            .GroupBy(d => d.Category)
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            var categoryGroup = new NodeCategoryGroup
            {
                Name = group.Key.ToString()
            };
            
            // Optional: Sub-group by SubCategory if needed, but for now flat list per category is clean
            foreach (var def in group.OrderBy(d => d.DisplayName))
            {
                categoryGroup.Definitions.Add(def);
            }
            
            NodeLibraryGroups.Add(categoryGroup);
        }
    }
    /// <summary>
    /// Called when user clicks a node definition in the library.
    /// </summary>
    [RelayCommand]
    private void AddNodeFromDefinition(NodeDefinition? definition)
    {
        if (definition == null) return;

        double offsetX = (Nodes.Count % 5) * 40;
        double offsetY = (Nodes.Count % 5) * 40;
        
        var newNodeVm = new NodeViewModel(definition, 200 + offsetX, 150 + offsetY);
        Nodes.Add(newNodeVm);
        RecalculateLayout();
        NodesChanged?.Invoke();
    }

    [RelayCommand]
    private void OpenMetadataEditor()
    {
        if (SelectedFile == null) return;

        var viewModel = new MetadataEditorViewModel(
            _metadataRepository,
            _serviceProvider.GetRequiredService<ILogger<MetadataEditorViewModel>>(),
            SelectedFile.Id,
            SelectedFile.FileName);

        var window = new MetadataEditorWindow
        {
            DataContext = viewModel
        };

        // 取得主視窗並顯示
        if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            window.Show(desktop.MainWindow);
        }
    }

    /// <summary>
    /// Refactored Execution Engine: Uses NodeDefinition Factory instead of hardcoded switch.
    /// </summary>
    private async Task ExecuteWorkflowFromDefinitionsAsync()
    {
        ExecutionLog = "Executing workflow...\n";
        ResultFiles.Clear();

        try
        {
            var sortedNodes = GetTopologicalOrder();
            var context = new NodeExecutionContext();

            foreach (var nodeVm in sortedNodes)
            {
                nodeVm.Status = "Running";
                
                // 1. Instantiate backend node via Definition Factory
                var backendNode = nodeVm.Definition.Factory(_serviceProvider);
                
                // 2. Inject UI parameters into backend node
                var paramDict = nodeVm.Parameters.ToDictionary(p => p.Key, p => p.Value);
                nodeVm.Definition.ApplyParameters?.Invoke(backendNode, paramDict);
                
                // 3. Execute
                await backendNode.ExecuteAsync(context, CancellationToken.None);
                
                nodeVm.Status = $"Success ({context.CurrentFileSet.Count})";
                ExecutionLog += $"[{nodeVm.Title}] Count: {context.CurrentFileSet.Count}\n";
            }

            foreach (var file in context.CurrentFileSet) ResultFiles.Add(file);
            ExecutionLog += $"Workflow completed. Total: {ResultFiles.Count} files\n";
        }
        catch (Exception ex) 
        { 
            ExecutionLog += $"Error: {ex.Message}\n"; 
        }
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
        // 輔助方法：從 Parameters 集合中安全取得數值
        string GetParam(string label, string fallback = "") 
        {
            var p = vm.Parameters.FirstOrDefault(x => x.Label == label);
            return p?.Value ?? fallback;
        }

        return vm.NodeType switch
        {
            "AllFiles" => new AllFilesNode(_fileRepository, _searchService, _previewService),
            "FolderScanner" => new FolderScannerNode(_fileRepository, _searchService, _previewService),
            "FilterTxt" => new FileTypeFilterNode(".txt"),
            "FilterMd" => new FileTypeFilterNode(".md"),
            "DynamicRule" => new DynamicRuleNode(GetParam("Rule", vm.ParameterValue)),
            "AddTagAI" => new AddTagNode(_metadataRepository, "AI"),
            "SetSubjectCS" => new SetSubjectNode(_metadataRepository, "Computer Science"),
            "AutoTag" => new AutoTagNode(_autoTaggingService),
            "ConditionBranch" => new ConditionBranchNode(GetParam("Rule", vm.ParameterValue)),
            "MergeBranches" => new MergeBranchesNode(),
            "DateRangeFilter" => new DateRangeFilterNode(GetParam("Date Rule", vm.ParameterValue)),
            "SizeFilter" => new SizeFilterNode(GetParam("Size Rule", vm.ParameterValue)),
            "TagFilter" => new TagFilterNode(_metadataRepository, GetParam("Target Tag", vm.ParameterValue)),
            "Sort" => new SortNode(GetParam("Sort Rule", vm.ParameterValue)),
            "Limit" => new LimitNode(int.TryParse(GetParam("Max Count", vm.ParameterValue), out int limit) ? limit : 100),
            "RegexSearch" => new RegexSearchNode(GetParam("Regex Rule", vm.ParameterValue)),
            
            // 修正：優先讀取 "Target File ID" 參數，如果沒有則讀取 ParameterValue
            "CreateRelationship" => new CreateRelationshipNode(
                _relationshipRepository, 
                GetParam("Target File ID", vm.ParameterValue), 
                GetParam("Relation Type", "References")),
                
            // 修正：優先讀取 "Source File ID" 參數
            "FindRelated" => new FindRelatedFilesNode(
                _relationshipRepository, 
                _fileRepository, 
                GetParam("Source File ID", vm.ParameterValue)),
                
            "Result" => new PassThroughNode(),
            "ExportCsv" => new ExportCsvNode(GetParam("Filename", vm.ParameterValue)),
            "ExportJson" => new ExportJsonNode(GetParam("Filename", vm.ParameterValue)),
            "ExportDcXml" => new ExportDublinCoreNode(_dcExportService, GetParam("Filename", vm.ParameterValue)),
            _ => throw new InvalidOperationException($"Unknown or unregistered node type: {vm.NodeType}")
        };
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
        AddNodeInternal(title, type, x, y, defaultParam);
    }

    private void AddNodeInternal(string title, string type, double x, double y, string defaultParam = "")
    {
        double offsetX = (Nodes.Count % 5) * 30;
        var newNode = new NodeViewModel(title, type, x + offsetX, y, defaultParam);

        // Initialize parameters based on node type
        switch (type)
        {
            case "FilterTxt":
            case "FilterMd":
                newNode.AddDropdownParam("Extension", ".txt", ".md", ".csv", ".json");
                break;
            case "DynamicRule":
                newNode.AddTextParam("Rule", defaultParam);
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
            case "ExportDcXml":
                newNode.AddTextParam("Filename", defaultParam);
                break;
            case "CreateRelationship":
                newNode.AddTextParam("Target File ID", defaultParam);
                newNode.AddDropdownParam("Relation Type", "References", "HasNote", "UsesAsset", "IsVersionOf");
                break;
            case "FindRelated":
                newNode.AddTextParam("Source File ID", defaultParam);
                break;
            case "DateRangeFilter":
                newNode.AddTextParam("Date Rule", defaultParam);
                break;
            case "SizeFilter":
                newNode.AddTextParam("Size Rule", defaultParam);
                break;
            case "TagFilter":
                newNode.AddTextParam("Target Tag", defaultParam);
                break;
            case "Sort":
                newNode.AddTextParam("Sort Rule", defaultParam);
                newNode.AddDropdownParam("Direction", "asc", "desc");
                break;
            case "Limit":
                newNode.AddNumberParam("Max Count", defaultParam);
                break;
            case "RegexSearch":
                newNode.AddTextParam("Regex Rule", defaultParam);
                break;
            default:
                newNode.AddTextParam("General Parameter", defaultParam);
                break;
        }

        // Subscribe to parameter changes for preview updates
        SubscribeToParameterChanges(newNode);

        Nodes.Add(newNode);
        RecalculateLayout();
        NodesChanged?.Invoke();
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

    public void SelectFile(FileRecord? file) 
    {
        SelectedFile = file; 
        SelectedFileMetadata.Clear();
        GroupedMetadata.Clear();
        
        if (file != null)
        {
            Task.Run(async () => 
            {
                var metadata = await _metadataRepository.GetMetadataByFileIdAsync(file.Id);
                var metaList = metadata.ToList();
                
                // 1. 填充原本的列表 (用於簡單顯示)
                foreach (var m in metaList) SelectedFileMetadata.Add(m);

                // 2. 分組填充 Editor (Basic, Descriptive, Personal, Technical)
                var grouped = metaList.GroupBy(m => m.Category);
                foreach (var group in grouped)
                {
                    var mg = new MetadataGroup { CategoryName = group.Key };
                    foreach (var item in group) mg.Fields.Add(item);
                    GroupedMetadata.Add(mg);
                }
                
                // 確保 UI 更新
                Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                    // Trigger property changed if necessary, usually ObservableCollection handles it
                });
            });
        }
    }

    // --- Execution ---
    [RelayCommand]
    private async Task ExecuteWorkflowAsync()
    {
        ExecutionLog = "Executing workflow...\n";
        ResultFiles.Clear();
        PendingPreviews.Clear();
        HasPendingActions = false;

        try
        {
            var sortedNodes = GetTopologicalOrder();
            var context = new NodeExecutionContext();
            var actionNodesToApply = new List<(NodeViewModel Vm, IActionNode Node)>();

            // Phase 1: Execute Query Nodes & Collect Action Nodes
            foreach (var nodeVm in sortedNodes)
            {
                var backendNode = CreateBackendNode(nodeVm);
                
                if (backendNode is IActionNode actionNode)
                {
                    // Don't execute yet, just collect for preview
                    var preview = await actionNode.PreviewAsync(context);
                    PendingPreviews.Add(preview);
                    actionNodesToApply.Add((nodeVm, actionNode));
                    nodeVm.Status = "Pending Apply";
                }
                else
                {
                    // Execute Query Nodes immediately
                    nodeVm.Status = "Running";
                    await backendNode.ExecuteAsync(context);
                    nodeVm.Status = $"Success ({context.CurrentFileSet.Count})";
                    ExecutionLog += $"[{nodeVm.Title}] Count: {context.CurrentFileSet.Count}\n";
                }
            }

            // Populate Result Table with the current file set (after all Query Nodes)
            foreach (var file in context.CurrentFileSet) ResultFiles.Add(file);

            // Phase 2: Check if there are pending actions
            if (actionNodesToApply.Count > 0)
            {
                HasPendingActions = true;
                ExecutionLog += $"\n--- PAUSED ---\n{actionNodesToApply.Count} Action(s) require your confirmation.\nReview the Preview panel and click 'Apply Changes' to proceed.\n";
            }
            else
            {
                ExecutionLog += "\nWorkflow completed successfully (Query Only).\n";
            }
        }
        catch (Exception ex) 
        { 
            ExecutionLog += $"Error: {ex.Message}\n"; 
        }
    }
    
    // 新增 Apply Changes Command
    [RelayCommand]
    private async Task ApplyPendingActionsAsync()
    {
        if (!HasPendingActions) return;

        ExecutionLog += "Applying changes...\n";
        var sortedNodes = GetTopologicalOrder();
        var context = new NodeExecutionContext();
        
        // Re-execute Query Nodes to restore context state
        foreach (var nodeVm in sortedNodes)
        {
            var backendNode = CreateBackendNode(nodeVm);
            if (backendNode is not IActionNode)
            {
                await backendNode.ExecuteAsync(context);
            }
        }

        // Execute Action Nodes
        foreach (var nodeVm in sortedNodes)
        {
            var backendNode = CreateBackendNode(nodeVm);
            if (backendNode is IActionNode actionNode)
            {
                nodeVm.Status = "Applying...";
                await actionNode.ApplyAsync(context);
                nodeVm.Status = "Applied";
                ExecutionLog += $"[{nodeVm.Title}] Applied successfully.\n";
                
                // TODO: Log to Batch Job Service here
            }
        }
        HasPendingActions = false;
        PendingPreviews.Clear();
        ExecutionLog += "All changes applied and saved.\n";
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

    /// <summary>
    /// Initializes the hierarchical node library tree with all available node types.
    /// </summary>
    private void InitializeNodeLibraryTree()
    {
        NodeLibraryTree.Clear();

        // 1. Sources
        var sources = new NodeLibraryItem("Sources", isCategory: true);
        sources.Children.Add(new NodeLibraryItem("All Files", "AllFiles"));
        sources.Children.Add(new NodeLibraryItem("Folder Scanner", "FolderScanner"));
        NodeLibraryTree.Add(sources);

        // 2. Processors (Query/Filter)
        var processors = new NodeLibraryItem("Processors", isCategory: true);
        
        var filters = new NodeLibraryItem("Filters", isCategory: true);
        filters.Children.Add(new NodeLibraryItem("File Type (.txt)", "FilterTxt"));
        filters.Children.Add(new NodeLibraryItem("File Type (.md)", "FilterMd"));
        filters.Children.Add(new NodeLibraryItem("Dynamic Rule", "DynamicRule"));
        filters.Children.Add(new NodeLibraryItem("Date Range", "DateRangeFilter"));
        filters.Children.Add(new NodeLibraryItem("File Size", "SizeFilter"));
        filters.Children.Add(new NodeLibraryItem("Tag Filter", "TagFilter"));
        processors.Children.Add(filters);

        var search = new NodeLibraryItem("Search", isCategory: true);
        search.Children.Add(new NodeLibraryItem("Full Text Search", "FullTextSearch"));
        search.Children.Add(new NodeLibraryItem("Regex Search", "RegexSearch"));
        processors.Children.Add(search);

        var transform = new NodeLibraryItem("Transform", isCategory: true);
        transform.Children.Add(new NodeLibraryItem("Sort", "Sort"));
        transform.Children.Add(new NodeLibraryItem("Limit", "Limit"));
        processors.Children.Add(transform);

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
        
        var relationships = new NodeLibraryItem("Relationships", isCategory: true);
        relationships.Children.Add(new NodeLibraryItem("Link to Target", "CreateRelationship"));
        relationships.Children.Add(new NodeLibraryItem("Find Related", "FindRelated"));
        actions.Children.Add(relationships);
        
        NodeLibraryTree.Add(actions);

        // 4. Outputs
        var outputs = new NodeLibraryItem("Outputs", isCategory: true);
        outputs.Children.Add(new NodeLibraryItem("Result Table", "Result"));
        outputs.Children.Add(new NodeLibraryItem("Export CSV", "ExportCsv"));
        outputs.Children.Add(new NodeLibraryItem("Export JSON", "ExportJson"));
        outputs.Children.Add(new NodeLibraryItem("Export Dublin Core", "ExportDcXml"));
        NodeLibraryTree.Add(outputs);
            
    }
    private void SubscribeToParameterChanges(NodeViewModel node)
    {
        foreach (var param in node.Parameters)
        {
            param.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NodeParameterViewModel.Value))
                {
                    // Update node preview when parameter changes
                    node.GetType().GetMethod("UpdatePreview", 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance)?
                        .Invoke(node, null);
                }
            };
        }
    }
}

public class MetadataGroup
{
    public string CategoryName { get; set; } = string.Empty;
    public ObservableCollection<MetadataValue> Fields { get; set; } = new();
}

public class PassThroughNode : IArchiveNode
{
    public Guid Id { get; } = Guid.NewGuid(); public string DisplayName => "Result"; public double X { get; set; } public double Y { get; set; }
    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
