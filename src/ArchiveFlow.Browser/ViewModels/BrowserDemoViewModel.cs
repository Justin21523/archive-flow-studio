using System.Collections.ObjectModel;
using ArchiveFlow.Application.DTOs;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Application.Nodes.Definitions;
using ArchiveFlow.Browser.Services;
using ArchiveFlow.Domain.Entities;
using ArchiveFlow.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArchiveFlow.Browser.ViewModels;

public sealed partial class BrowserDemoViewModel : ObservableObject
{
    private readonly IDataRepository _repository;
    private readonly IDemoDataService _demoDataService;
    private readonly IImportPipelineService _importPipelineService;
    private readonly IExportService _exportService;
    private readonly BrowserDemoDataStore _dataStore;

    public ObservableCollection<BrowserFileRow> Files { get; } = [];
    public ObservableCollection<BrowserFileRow> FilteredFiles { get; } = [];
    public ObservableCollection<MetadataValue> SelectedMetadata { get; } = [];
    public ObservableCollection<ImportPreviewItem> ImportPreviewItems { get; } = [];
    public ObservableCollection<FileRelationship> Relationships { get; } = [];
    public ObservableCollection<ExportJobRecord> ExportJobs { get; } = [];
    public ObservableCollection<ImportJobRecord> ImportJobs { get; } = [];
    public ObservableCollection<BrowserNodeLibraryGroup> NodeLibraryGroups { get; } = [];
    public ObservableCollection<BrowserWorkflowNode> WorkflowNodes { get; } = [];
    public ObservableCollection<BrowserWorkflowEdge> WorkflowEdges { get; } = [];
    public ObservableCollection<BrowserWorkflowGroup> WorkflowGroups { get; } = [];
    public ObservableCollection<BrowserPendingChange> PendingChanges { get; } = [];

    private int _nextNodeNumber = 1;
    private int _nextGroupNumber = 1;
    private BrowserWorkflowNode? _pendingConnectionSource;
    private readonly Dictionary<string, int> _lastNodeCounts = new(StringComparer.Ordinal);

    public IReadOnlyList<string> Scenarios { get; } =
    [
        "Research Review",
        "Design Asset Mapping",
        "Metadata Cleanup"
    ];

    public IReadOnlyList<string> ExtensionFilters { get; } =
    [
        "All",
        ".pdf",
        ".md",
        ".csv",
        ".png",
        ".glb",
        ".docx",
        ".txt",
        ".bib",
        ".jpg",
        ".mp4",
        ".mp3",
        ".zip"
    ];

    public IReadOnlyList<string> ExportFormats { get; } =
    [
        "CSV",
        "JSON",
        "Dublin Core XML"
    ];

    public IReadOnlyList<string> RelationshipTypes { get; } =
    [
        "references",
        "cites",
        "describes",
        "is-derived-from",
        "uses-asset",
        "part-of"
    ];

    [ObservableProperty]
    private string _selectedScenario = "Research Review";

    [ObservableProperty]
    private string _selectedExtensionFilter = "All";

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private BrowserFileRow? _selectedFile;

    [ObservableProperty]
    private BrowserFileRow? _sourceFile;

    [ObservableProperty]
    private BrowserFileRow? _targetFile;

    [ObservableProperty]
    private string _selectedRelationshipType = "references";

    [ObservableProperty]
    private string _selectedExportFormat = "CSV";

    [ObservableProperty]
    private string _statusMessage = "Browser demo loaded.";

    [ObservableProperty]
    private string _exportPreview = string.Empty;

    [ObservableProperty]
    private string _importSummary = "No import preview yet.";

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _filteredCount;

    [ObservableProperty]
    private int _relationshipCount;

    [ObservableProperty]
    private double _canvasZoom = 1;

    [ObservableProperty]
    private double _canvasOffsetX = 0;

    [ObservableProperty]
    private double _canvasOffsetY = 0;

    [ObservableProperty]
    private BrowserWorkflowNode? _selectedWorkflowNode;

    [ObservableProperty]
    private BrowserWorkflowEdge? _selectedWorkflowEdge;

    [ObservableProperty]
    private string _inspectorTitle = "Workspace Overview";

    [ObservableProperty]
    private string _inspectorDescription = "Select a node to inspect its parameters and preview how it affects the demo workflow.";

    [ObservableProperty]
    private string _workflowSummary = "Ready to execute the browser workspace demo.";

    [ObservableProperty]
    private string _selectedNodeKind = "Workspace";

    [ObservableProperty]
    private string _selectedNodeOutput = "No node selected.";

    [ObservableProperty]
    private string _connectionStatus = "Click a node output dot, then click a target input dot to connect.";

    public string DemoNotice =>
        "Browser Workspace Demo: local folder scanning, native SQLite storage, and direct file-system export are simulated for online review.";

    public string DesktopNotice => "Desktop Full Version is available from GitHub Releases.";

    public BrowserDemoViewModel(
        IDataRepository repository,
        IDemoDataService demoDataService,
        IImportPipelineService importPipelineService,
        IExportService exportService,
        BrowserDemoDataStore dataStore)
    {
        _repository = repository;
        _demoDataService = demoDataService;
        _importPipelineService = importPipelineService;
        _exportService = exportService;
        _dataStore = dataStore;
        BuildWorkspaceDemo();
        _ = RefreshAllAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedExtensionFilterChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedFileChanged(BrowserFileRow? value)
    {
        _ = LoadSelectedMetadataAsync(value);
    }

    partial void OnSelectedWorkflowNodeChanged(BrowserWorkflowNode? value)
    {
        foreach (var node in WorkflowNodes)
        {
            node.IsSelected = ReferenceEquals(node, value);
        }

        if (value is not null)
        {
            SelectedWorkflowEdge = null;
        }

        InspectorTitle = value?.Title ?? "Workspace Overview";
        InspectorDescription = value?.Description ?? "Select a node to inspect its parameters and preview how it affects the demo workflow.";
        SelectedNodeKind = value?.Kind ?? "Workspace";
        SelectedNodeOutput = value is null
            ? $"Current result contains {FilteredFiles.Count} files. Use the demo nodes to understand source, filter, search, metadata, and output behavior."
            : value.OutputLabel;
    }

    partial void OnSelectedWorkflowEdgeChanged(BrowserWorkflowEdge? value)
    {
        foreach (var edge in WorkflowEdges)
        {
            edge.IsSelected = ReferenceEquals(edge, value);
        }

        if (value is null)
        {
            return;
        }

        SelectedWorkflowNode = null;
        InspectorTitle = "Connection";
        InspectorDescription = $"{value.Source.Title} sends its output to {value.Target.Title}.";
        SelectedNodeKind = "Workflow Edge";
        SelectedNodeOutput = "Delete this connection or select nodes to create a different route.";
    }

    [RelayCommand]
    private async Task ResetDemoDataAsync()
    {
        await _demoDataService.ResetAsync();
        ImportPreviewItems.Clear();
        ExportPreview = string.Empty;
        ImportSummary = "Demo data reset.";
        StatusMessage = "Demo data reset.";
        await RefreshAllAsync();
        UpdateWorkflowCounts();
    }

    [RelayCommand]
    private async Task LoadScenarioAsync()
    {
        await _demoDataService.LoadScenarioAsync(SelectedScenario);
        ImportPreviewItems.Clear();
        ExportPreview = string.Empty;
        ImportSummary = $"{SelectedScenario} scenario loaded.";
        StatusMessage = $"{SelectedScenario} scenario loaded.";
        await RefreshAllAsync();
        UpdateWorkflowCounts();
    }

    [RelayCommand]
    private async Task PreviewImportAsync()
    {
        var preview = await _importPipelineService.PreviewFolderAsync("browser-demo://sample-import", recursive: true);
        ImportPreviewItems.Clear();
        foreach (var item in preview.Items)
        {
            ImportPreviewItems.Add(item);
        }

        ImportSummary = $"Preview: {preview.NewCount} new, {preview.DuplicateCount} duplicate, {preview.ExistingCount} existing.";
        StatusMessage = "Mock import preview generated.";
    }

    [RelayCommand]
    private async Task ConfirmImportAsync()
    {
        if (ImportPreviewItems.Count == 0)
        {
            StatusMessage = "Generate an import preview first.";
            return;
        }

        var preview = new ImportPreviewResult
        {
            JobId = Guid.NewGuid().ToString("N"),
            FolderPath = "browser-demo://sample-import",
            Recursive = true,
            Items = ImportPreviewItems.ToList()
        };

        var result = await _importPipelineService.ApplyImportAsync(preview);
        ImportSummary = result.Summary;
        StatusMessage = result.Summary;
        await RefreshAllAsync();
    }

    [RelayCommand]
    private async Task CreateRelationshipAsync()
    {
        if (SourceFile is null || TargetFile is null)
        {
            StatusMessage = "Choose both source and target files.";
            return;
        }

        var created = await _repository.Relationships.TryCreateRelationshipAsync(
            SourceFile.Id,
            TargetFile.Id,
            SelectedRelationshipType);

        StatusMessage = created
            ? $"Relationship created: {SourceFile.FileName} -> {TargetFile.FileName}"
            : "Relationship was not created. Check source, target, and duplicate relationship type.";
        await RefreshRelationshipsAsync();
    }

    [RelayCommand]
    private async Task ExecuteWorkflowAsync()
    {
        PendingChanges.Clear();
        ExportPreview = string.Empty;
        _lastNodeCounts.Clear();

        if (WorkflowNodes.Count == 0)
        {
            SetFilteredFiles([]);
            WorkflowSummary = "No workflow nodes are available. Add a source node from the library.";
            StatusMessage = WorkflowSummary;
            return;
        }

        var orderedNodes = GetExecutionOrder();
        var hadCycleFallback = orderedNodes.Count != WorkflowNodes.Count;
        if (hadCycleFallback)
        {
            orderedNodes = WorkflowNodes.ToList();
        }

        var outputs = new Dictionary<BrowserWorkflowNode, List<BrowserFileRow>>();
        foreach (var node in orderedNodes)
        {
            var inputSets = GetInputSetsForNode(node, outputs);
            var input = MergeInputSets(node, inputSets);
            var result = await ExecuteWorkflowNodeAsync(node, input, inputSets);
            outputs[node] = result.Files;
            _lastNodeCounts[node.Id] = result.Files.Count;
            node.OutputLabel = result.OutputLabel;
        }

        var terminalNode = GetTerminalNode(orderedNodes, outputs);
        var finalFiles = terminalNode is not null && outputs.TryGetValue(terminalNode, out var terminalOutput)
            ? terminalOutput
            : [];

        SetFilteredFiles(finalFiles);
        SelectedFile = FilteredFiles.FirstOrDefault();

        var sideEffectText = PendingChanges.Count == 0
            ? string.Empty
            : $" {PendingChanges.Count} pending changes are ready to apply.";
        var cycleText = hadCycleFallback
            ? " The graph contains a cycle, so the browser demo used visual node order."
            : string.Empty;
        WorkflowSummary = $"Workflow executed through {orderedNodes.Count} nodes and {WorkflowEdges.Count} connections. {FilteredFiles.Count} files reached {terminalNode?.Title ?? "the result"}." + sideEffectText + cycleText;
        StatusMessage = WorkflowSummary;
        OnSelectedWorkflowNodeChanged(SelectedWorkflowNode);
    }

    [RelayCommand]
    private void AddNode(BrowserNodeLibraryItem? item)
    {
        if (item is null)
        {
            StatusMessage = "Choose a node type from the node library.";
            return;
        }

        var anchor = SelectedWorkflowNode ?? WorkflowNodes.LastOrDefault();
        var x = anchor is null ? 80 : Math.Min(1320, anchor.X + 180);
        var y = anchor is null ? 120 : anchor.Y + (WorkflowNodes.Count % 2 == 0 ? 135 : 0);
        var node = item.CreateNode($"demo-node-{_nextNodeNumber++}", x, Math.Min(760, y));

        WorkflowNodes.Add(node);
        if (anchor is not null && !ReferenceEquals(anchor, node))
        {
            AddEdge(anchor, node);
        }

        SelectedWorkflowNode = node;
        StatusMessage = $"Added node: {node.Title}.";
        UpdateWorkflowCounts();
    }

    [RelayCommand]
    private void DuplicateSelectedNode()
    {
        if (SelectedWorkflowNode is null)
        {
            StatusMessage = "Select a node to duplicate.";
            return;
        }

        var source = SelectedWorkflowNode;
        var copy = source.Clone(
            $"demo-node-{_nextNodeNumber++}",
            $"{source.Title} Copy",
            Math.Min(1320, source.X + 45),
            Math.Min(760, source.Y + 145));

        WorkflowNodes.Add(copy);
        AddEdge(source, copy);
        SelectedWorkflowNode = copy;
        StatusMessage = $"Duplicated node: {source.Title}.";
        UpdateWorkflowCounts();
    }

    [RelayCommand]
    private void DeleteSelectedNode()
    {
        if (SelectedWorkflowNode is null)
        {
            StatusMessage = "Select a node to delete.";
            return;
        }

        var node = SelectedWorkflowNode;
        foreach (var edge in WorkflowEdges.Where(edge => ReferenceEquals(edge.Source, node) || ReferenceEquals(edge.Target, node)).ToList())
        {
            WorkflowEdges.Remove(edge);
        }

        foreach (var group in WorkflowGroups.Where(group => group.Contains(node)).ToList())
        {
            WorkflowGroups.Remove(group);
        }

        WorkflowNodes.Remove(node);
        SelectedWorkflowNode = WorkflowNodes.FirstOrDefault();
        StatusMessage = $"Deleted node: {node.Title}.";
        UpdateWorkflowCounts();
    }

    [RelayCommand]
    private void MarkConnectionSource()
    {
        if (SelectedWorkflowNode is null)
        {
            ConnectionStatus = "Select a node before marking a link source.";
            return;
        }

        BeginPortConnection(SelectedWorkflowNode);
    }

    [RelayCommand]
    private void ConnectToSelectedNode()
    {
        if (_pendingConnectionSource is null || SelectedWorkflowNode is null)
        {
            ConnectionStatus = "Mark a source node, then select a target node.";
            StatusMessage = ConnectionStatus;
            return;
        }

        CompletePortConnection(SelectedWorkflowNode);
    }

    [RelayCommand]
    private void DeleteSelectedConnection()
    {
        if (SelectedWorkflowEdge is null)
        {
            StatusMessage = "Select a connection line to delete.";
            return;
        }

        var edge = SelectedWorkflowEdge;
        WorkflowEdges.Remove(edge);
        SelectedWorkflowEdge = null;
        StatusMessage = $"Deleted connection: {edge.Source.Title} -> {edge.Target.Title}.";
        UpdateWorkflowCounts();
    }

    [RelayCommand]
    private void GrowSelectedNode()
    {
        ResizeSelectedNode(24, 14);
    }

    [RelayCommand]
    private void ShrinkSelectedNode()
    {
        ResizeSelectedNode(-24, -14);
    }

    [RelayCommand]
    private void CreateGroupFromSelectedNode()
    {
        if (SelectedWorkflowNode is null)
        {
            StatusMessage = "Select a node before creating a group.";
            return;
        }

        var seed = SelectedWorkflowNode;
        var groupedNodes = WorkflowEdges
            .Where(edge => ReferenceEquals(edge.Source, seed) || ReferenceEquals(edge.Target, seed))
            .SelectMany(edge => new[] { edge.Source, edge.Target })
            .Append(seed)
            .Distinct()
            .ToList();

        var group = BrowserWorkflowGroup.FromNodes(
            $"group-{_nextGroupNumber}",
            $"Workflow Group {_nextGroupNumber++}",
            groupedNodes);
        WorkflowGroups.Add(group);
        StatusMessage = $"Created group: {group.Name}.";
    }

    [RelayCommand]
    private void ClearGroups()
    {
        WorkflowGroups.Clear();
        StatusMessage = "Workflow groups cleared.";
    }

    [RelayCommand]
    private void ResetWorkflow()
    {
        BuildWorkspaceDemo();
        ApplyFilter();
        StatusMessage = "Workflow reset to the default browser demo.";
        WorkflowSummary = "Default browser workspace restored.";
        UpdateWorkflowCounts();
    }

    [RelayCommand]
    private async Task ApplyPendingChangesAsync()
    {
        if (PendingChanges.Count == 0)
        {
            StatusMessage = "No pending metadata changes to apply.";
            return;
        }

        foreach (var change in PendingChanges.ToList())
        {
            if (change.Action == "Set status")
            {
                var record = await _repository.Files.GetByIdAsync(change.FileId);
                if (record is not null)
                {
                    record.UpdateStatus(Enum.TryParse<FileStatus>(change.Value, ignoreCase: true, out var status)
                        ? status
                        : FileStatus.Archived);
                    await _repository.Files.SaveAsync(record);
                    await _repository.Metadata.SetMetadataValueAsync(
                        change.FileId,
                        "status",
                        "Status",
                        "String",
                        "Basic",
                        change.Value);
                }

                continue;
            }

            switch (change.Action)
            {
                case "Remove tag":
                    await _repository.Metadata.DeleteMetadataValueAsync(change.FileId, "tag", change.Value);
                    break;
                case "Set subject":
                    await _repository.Metadata.SetMetadataValueAsync(change.FileId, "subject", "Subject", "String", "Descriptive", change.Value);
                    break;
                case "Set project":
                    await _repository.Metadata.SetMetadataValueAsync(change.FileId, "project", "Project", "String", "Descriptive", change.Value);
                    break;
                case "Set reading status":
                    await _repository.Metadata.SetMetadataValueAsync(change.FileId, "reading_status", "Reading Status", "String", "Personal", change.Value);
                    break;
                case "Set importance":
                    await _repository.Metadata.SetMetadataValueAsync(change.FileId, "importance", "Importance", "String", "Personal", change.Value);
                    break;
                case "Generate archive id":
                case "Validate metadata":
                    await _repository.Metadata.SetMetadataValueAsync(change.FileId, "workflow_note", "Workflow Note", "String", "Technical", change.Value);
                    break;
                default:
                    await _repository.Metadata.AddMetadataValueIfMissingAsync(
                        change.FileId,
                        "tag",
                        "Tag",
                        "String",
                        "Descriptive",
                        change.Value);
                    break;
            }
        }

        PendingChanges.Clear();
        StatusMessage = "Browser demo metadata changes applied in memory.";
        await RefreshFilesAsync();
        OnSelectedWorkflowNodeChanged(SelectedWorkflowNode);
    }

    [RelayCommand]
    private void ResetCanvasView()
    {
        CanvasZoom = 1;
        CanvasOffsetX = 0;
        CanvasOffsetY = 0;
        StatusMessage = "Canvas view reset.";
    }

    [RelayCommand]
    private async Task ExportFilteredAsync()
    {
        var files = FilteredFiles.Select(row => row.Record).ToList();
        if (files.Count == 0)
        {
            StatusMessage = "No files to export.";
            return;
        }

        var format = SelectedExportFormat switch
        {
            "JSON" => ExportFormat.Json,
            "Dublin Core XML" => ExportFormat.DublinCoreXml,
            _ => ExportFormat.Csv
        };

        var result = await _exportService.ExportAsync(new ExportRequest
        {
            Format = format,
            Files = files,
            RequestedFileName = $"archiveflow-demo-{DateTime.UtcNow:yyyyMMddHHmmss}"
        });

        ExportPreview = _dataStore.LastExportContent;
        StatusMessage = result.Message;
        await RefreshJobsAsync();
        UpdateWorkflowCounts();
    }

    public void SelectWorkflowNode(BrowserWorkflowNode node)
    {
        SelectedWorkflowNode = node;
    }

    public void SelectWorkflowEdge(BrowserWorkflowEdge edge)
    {
        SelectedWorkflowEdge = edge;
    }

    public void BeginPortConnection(BrowserWorkflowNode source)
    {
        SelectWorkflowNode(source);
        if (!source.CanStartOutput)
        {
            ConnectionStatus = $"{source.Title} has no output port.";
            StatusMessage = ConnectionStatus;
            return;
        }

        _pendingConnectionSource = source;
        ConnectionStatus = $"Link source: {source.Title}. Click another node input dot to connect.";
        StatusMessage = ConnectionStatus;
    }

    public void CompletePortConnection(BrowserWorkflowNode target)
    {
        if (_pendingConnectionSource is null)
        {
            ConnectionStatus = "Click an output dot first, then click a target input dot.";
            StatusMessage = ConnectionStatus;
            return;
        }

        if (!target.CanAcceptInput)
        {
            ConnectionStatus = $"{target.Title} has no input port.";
            StatusMessage = ConnectionStatus;
            return;
        }

        if (ReferenceEquals(_pendingConnectionSource, target))
        {
            ConnectionStatus = "A node cannot connect to itself.";
            StatusMessage = ConnectionStatus;
            return;
        }

        if (AddEdge(_pendingConnectionSource, target))
        {
            ConnectionStatus = $"Connected {_pendingConnectionSource.Title} -> {target.Title}.";
            SelectWorkflowNode(target);
        }
        else
        {
            ConnectionStatus = "That connection already exists.";
        }

        _pendingConnectionSource = null;
        StatusMessage = ConnectionStatus;
        UpdateWorkflowCounts();
    }

    public void MoveWorkflowNode(BrowserWorkflowNode node, double x, double y)
    {
        node.X = Math.Max(20, Math.Min(1420, x));
        node.Y = Math.Max(20, Math.Min(820, y));
        foreach (var group in WorkflowGroups.Where(group => group.Contains(node)))
        {
            group.Recalculate();
        }

        RecalculateEdges();
    }

    public void PanCanvas(double deltaX, double deltaY)
    {
        CanvasOffsetX += deltaX;
        CanvasOffsetY += deltaY;
    }

    public void ZoomCanvas(double factor)
    {
        CanvasZoom = Math.Max(0.55, Math.Min(1.8, CanvasZoom * factor));
    }

    private async Task RefreshAllAsync()
    {
        await RefreshFilesAsync();
        await RefreshRelationshipsAsync();
        await RefreshJobsAsync();
    }

    private async Task RefreshFilesAsync()
    {
        Files.Clear();
        var files = await _repository.Files.GetAllAsync();
        foreach (var file in files)
        {
            var metadata = await _repository.Metadata.GetMetadataByFileIdAsync(file.Id);
            Files.Add(new BrowserFileRow(file, metadata));
        }

        TotalCount = Files.Count;
        ApplyFilter();
        SelectedFile ??= FilteredFiles.FirstOrDefault();
        SourceFile ??= FilteredFiles.FirstOrDefault();
        TargetFile ??= FilteredFiles.Skip(1).FirstOrDefault();
    }

    private async Task LoadSelectedMetadataAsync(BrowserFileRow? file)
    {
        SelectedMetadata.Clear();
        if (file is null)
        {
            return;
        }

        var metadata = await _repository.Metadata.GetMetadataByFileIdAsync(file.Id);
        foreach (var item in metadata)
        {
            SelectedMetadata.Add(item);
        }
    }

    private void ApplyFilter()
    {
        FilteredFiles.Clear();
        var query = SearchText.Trim();
        var filtered = Files.Where(row =>
            (SelectedExtensionFilter == "All" || row.Extension == SelectedExtensionFilter) &&
            (string.IsNullOrWhiteSpace(query) ||
             row.FileName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             row.Path.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             row.MetadataSummary.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             row.Preview.Contains(query, StringComparison.OrdinalIgnoreCase)));

        foreach (var row in filtered)
        {
            FilteredFiles.Add(row);
        }

        FilteredCount = FilteredFiles.Count;
        UpdateWorkflowCounts();
    }

    private void SetFilteredFiles(IEnumerable<BrowserFileRow> rows)
    {
        FilteredFiles.Clear();
        foreach (var row in rows.GroupBy(row => row.Id).Select(group => group.First()))
        {
            FilteredFiles.Add(row);
        }

        FilteredCount = FilteredFiles.Count;
    }

    private List<BrowserWorkflowNode> GetExecutionOrder()
    {
        var incomingCounts = WorkflowNodes.ToDictionary(node => node, _ => 0);
        foreach (var edge in WorkflowEdges)
        {
            if (incomingCounts.ContainsKey(edge.Target))
            {
                incomingCounts[edge.Target]++;
            }
        }

        var ready = new Queue<BrowserWorkflowNode>(WorkflowNodes.Where(node => incomingCounts[node] == 0));
        var ordered = new List<BrowserWorkflowNode>();
        while (ready.Count > 0)
        {
            var node = ready.Dequeue();
            ordered.Add(node);

            foreach (var edge in WorkflowEdges.Where(edge => ReferenceEquals(edge.Source, node)).ToList())
            {
                incomingCounts[edge.Target]--;
                if (incomingCounts[edge.Target] == 0)
                {
                    ready.Enqueue(edge.Target);
                }
            }
        }

        return ordered;
    }

    private List<List<BrowserFileRow>> GetInputSetsForNode(BrowserWorkflowNode node, IReadOnlyDictionary<BrowserWorkflowNode, List<BrowserFileRow>> outputs)
    {
        return WorkflowEdges
            .Where(edge => ReferenceEquals(edge.Target, node))
            .Select(edge => outputs.TryGetValue(edge.Source, out var rows) ? rows : [])
            .Where(rows => rows.Count > 0)
            .ToList();
    }

    private List<BrowserFileRow> MergeInputSets(BrowserWorkflowNode node, IReadOnlyList<List<BrowserFileRow>> inputSets)
    {
        var upstreamRows = inputSets
            .SelectMany(rows => rows)
            .DistinctBy(row => row.Id)
            .ToList();

        if (upstreamRows.Count > 0 || IsSourceNode(node))
        {
            return upstreamRows;
        }

        // 瀏覽器 demo 允許單獨拖曳 filter/search/output 節點進行試用。
        return Files.ToList();
    }

    private BrowserWorkflowNode? GetTerminalNode(IReadOnlyList<BrowserWorkflowNode> orderedNodes, IReadOnlyDictionary<BrowserWorkflowNode, List<BrowserFileRow>> outputs)
    {
        return orderedNodes.LastOrDefault(node => node.Title.Contains("Result", StringComparison.OrdinalIgnoreCase)) ??
               orderedNodes.LastOrDefault(node => node.Kind == "Outputs") ??
               orderedNodes.LastOrDefault(node => !WorkflowEdges.Any(edge => ReferenceEquals(edge.Source, node)) && outputs.ContainsKey(node)) ??
               orderedNodes.LastOrDefault();
    }

    private async Task<BrowserNodeExecutionResult> ExecuteWorkflowNodeAsync(
        BrowserWorkflowNode node,
        List<BrowserFileRow> input,
        IReadOnlyList<List<BrowserFileRow>> inputSets)
    {
        if (IsSourceNode(node))
        {
            return ExecuteSourceNode(node);
        }

        if (node.Kind == "Query Filters")
        {
            return ExecuteFilterNode(node, input);
        }

        if (node.Kind == "Search")
        {
            return ExecuteSearchNode(node, input);
        }

        if (node.Kind == "Logic & Set Operations")
        {
            return ExecuteLogicNode(node, input, inputSets);
        }

        if (node.Kind == "Metadata Actions")
        {
            return ExecuteMetadataActionNode(node, input);
        }

        if (node.Kind == "Relationships")
        {
            return await ExecuteRelationshipNodeAsync(node, input);
        }

        if (node.Kind == "Indexing & Extraction" || node.Kind == "File Actions" || node.Kind == "Create / Template")
        {
            return ExecuteBrowserSafeActionNode(node, input);
        }

        if (node.Kind == "Outputs")
        {
            return await ExecuteOutputNodeAsync(node, input);
        }

        return new BrowserNodeExecutionResult(input, $"{input.Count} files passed through {node.Title}.");
    }

    private BrowserNodeExecutionResult ExecuteSourceNode(BrowserWorkflowNode node)
    {
        var output = node.NodeType switch
        {
            "source.recent_imports" => Files
                .OrderByDescending(row => row.Record.ImportedAt)
                .Take(Math.Min(20, Files.Count))
                .ToList(),
            "source.selected_files" => SelectedFile is null ? Files.Take(10).ToList() : [SelectedFile],
            "source.unorganized_files" => Files
                .Where(row => IsMissingDescriptiveMetadata(row) ||
                              row.MetadataValues.All(value => value.FieldName != "project") ||
                              row.MetadataValues.All(value => value.FieldName != "tag"))
                .ToList(),
            "source.missing_metadata" => Files
                .Where(IsMissingDescriptiveMetadata)
                .ToList(),
            "source.duplicate_files" => Files
                .GroupBy(row => row.Record.FileHash, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .SelectMany(group => group)
                .ToList(),
            "source.folder_source" => FilterByText(Files, GetParameter(node, "folderPath"), row => row.Path).ToList(),
            _ => Files.ToList()
        };

        return new BrowserNodeExecutionResult(output, $"{output.Count} files loaded by {node.Title}.");
    }

    private BrowserNodeExecutionResult ExecuteFilterNode(BrowserWorkflowNode node, List<BrowserFileRow> input)
    {
        var output = node.NodeType switch
        {
            "filter.file_type" => input.Where(row => MatchesFileType(row, GetParameter(node, "fileType", "Document"))).ToList(),
            "filter.extension" => FilterByExtension(input, GetParameter(node, "extension", ".pdf")).ToList(),
            "filter.date_range" => input.Where(row => MatchesDateRange(row, GetParameter(node, "startDate"), GetParameter(node, "endDate"))).ToList(),
            "filter.size" => input.Where(row => MatchesSizeRange(row, GetParameter(node, "minSizeMb", "0"), GetParameter(node, "maxSizeMb", "100"))).ToList(),
            "filter.path_contains" => FilterByText(input, GetParameter(node, "pathText"), row => row.Path).ToList(),
            "filter.tag" => input.Where(row => HasMetadata(row, "tag", GetParameter(node, "tag"))).ToList(),
            "filter.subject" => input.Where(row => HasMetadata(row, "subject", GetParameter(node, "subject"))).ToList(),
            "filter.metadata_field" => input
                .Where(row => MatchesMetadataRule(
                    row,
                    GetParameter(node, "fieldName", "subject"),
                    GetParameter(node, "operator", "contains"),
                    GetParameter(node, "value")))
                .ToList(),
            "filter.status" => input.Where(row => MatchesStatus(row, GetParameter(node, "status", "Scanned"))).ToList(),
            _ => input.ToList()
        };

        return new BrowserNodeExecutionResult(output, $"{output.Count} of {input.Count} files passed {node.Title}.");
    }

    private BrowserNodeExecutionResult ExecuteSearchNode(BrowserWorkflowNode node, List<BrowserFileRow> input)
    {
        var query = GetSearchQuery(node);
        var output = node.NodeType switch
        {
            "search.full_text" => ExecuteFullTextSearch(input, query, GetParameter(node, "mode", "All terms")),
            "search.boolean" => ExecuteBooleanSearch(input, string.IsNullOrWhiteSpace(query) ? "AI AND metadata" : query),
            "search.filename" => FilterByText(input, query, row => row.FileName).ToList(),
            "search.content" => FilterByText(input, query, row => row.Preview).ToList(),
            _ => string.IsNullOrWhiteSpace(query)
                ? input.ToList()
                : input.Where(row => BuildSearchText(row).Contains(query, StringComparison.OrdinalIgnoreCase)).ToList()
        };

        var labelQuery = string.IsNullOrWhiteSpace(query)
            ? "default demo query"
            : $"\"{query}\"";
        return new BrowserNodeExecutionResult(output, $"{output.Count} of {input.Count} files matched {node.Title} using {labelQuery}.");
    }

    private BrowserNodeExecutionResult ExecuteLogicNode(
        BrowserWorkflowNode node,
        List<BrowserFileRow> input,
        IReadOnlyList<List<BrowserFileRow>> inputSets)
    {
        var output = node.NodeType switch
        {
            "logic.and" or "logic.intersection" => IntersectInputs(inputSets),
            "logic.or" or "logic.union" => inputSets.SelectMany(rows => rows).DistinctBy(row => row.Id).ToList(),
            "logic.difference" => DifferenceInputs(inputSets),
            "logic.not" => Files.Where(row => input.All(inputRow => inputRow.Id != row.Id)).ToList(),
            "logic.sort_by" => SortRows(input, GetParameter(node, "field", "Imported Date"), GetParameter(node, "direction", "Descending")).ToList(),
            "logic.limit" => input.Take(ParseInt(GetParameter(node, "count", "100"), 100)).ToList(),
            _ => input.ToList()
        };

        var groupText = node.NodeType == "logic.group_by"
            ? $" Grouped by {GetParameter(node, "field", "Extension")} in the demo summary."
            : string.Empty;
        return new BrowserNodeExecutionResult(output, $"{output.Count} files produced by {node.Title}.{groupText}");
    }

    private BrowserNodeExecutionResult ExecuteMetadataActionNode(BrowserWorkflowNode node, List<BrowserFileRow> input)
    {
        var (action, value) = node.NodeType switch
        {
            "metadata.remove_tag" => ("Remove tag", GetParameter(node, "tag")),
            "metadata.set_subject" => ("Set subject", GetParameter(node, "subject", "Browser Demo")),
            "metadata.set_project" => ("Set project", GetParameter(node, "project", "ArchiveFlow Demo")),
            "metadata.set_status" => ("Set status", GetParameter(node, "status", "Archived")),
            "metadata.set_reading_status" => ("Set reading status", GetParameter(node, "readingStatus", "To Read")),
            "metadata.set_importance" => ("Set importance", GetParameter(node, "importance", "Normal")),
            "metadata.generate_archive_id" => ("Generate archive id", GetParameter(node, "pattern", "AF-{yyyyMMdd}-{sequence}")),
            "metadata.validate_metadata" => ("Validate metadata", "Completeness check"),
            _ => ("Add tag", GetParameter(node, "tag", "AI"))
        };

        foreach (var file in input)
        {
            PendingChanges.Add(new BrowserPendingChange(file.Id, file.FileName, action, value));
        }

        return new BrowserNodeExecutionResult(input, $"{input.Count} files passed through; {input.Count} pending {action.ToLowerInvariant()} changes created.");
    }

    private async Task<BrowserNodeExecutionResult> ExecuteRelationshipNodeAsync(BrowserWorkflowNode node, List<BrowserFileRow> input)
    {
        if (node.NodeType is "relationship.find_related" or "relationship.find_notes")
        {
            var sourceIds = input.Select(row => row.Id).ToHashSet(StringComparer.Ordinal);
            var relationships = await _repository.Relationships.GetAllRelationshipsAsync();
            var relatedIds = relationships
                .Where(item => sourceIds.Contains(item.SourceFileId) || sourceIds.Contains(item.TargetFileId))
                .SelectMany(item => new[] { item.SourceFileId, item.TargetFileId })
                .Where(id => !sourceIds.Contains(id))
                .ToHashSet(StringComparer.Ordinal);
            var related = Files.Where(row => relatedIds.Contains(row.Id)).ToList();
            return new BrowserNodeExecutionResult(related, $"{related.Count} related files found from {input.Count} input files.");
        }

        if (node.NodeType is "relationship.same_project" or "relationship.same_subject")
        {
            var field = node.NodeType == "relationship.same_project" ? "project" : "subject";
            var values = input
                .SelectMany(row => row.MetadataValues)
                .Where(value => string.Equals(value.FieldName, field, StringComparison.OrdinalIgnoreCase))
                .Select(value => value.ValueText)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var matches = Files.Where(row => row.MetadataValues.Any(value =>
                string.Equals(value.FieldName, field, StringComparison.OrdinalIgnoreCase) &&
                values.Contains(value.ValueText))).ToList();
            return new BrowserNodeExecutionResult(matches, $"{matches.Count} files share the same {field}.");
        }

        if (node.NodeType == "relationship.build_graph")
        {
            await RefreshRelationshipsAsync();
            return new BrowserNodeExecutionResult(input, $"Graph view prepared with {Relationships.Count} relationship records.");
        }

        var source = SourceFile ?? input.FirstOrDefault();
        var targetArchiveId = GetParameter(node, "targetArchiveId");
        var target = Files.FirstOrDefault(row => string.Equals(row.ArchiveId, targetArchiveId, StringComparison.OrdinalIgnoreCase)) ??
                     TargetFile ??
                     input.Skip(1).FirstOrDefault() ??
                     Files.FirstOrDefault(row => source is null || row.Id != source.Id);
        if (source is null || target is null)
        {
            return new BrowserNodeExecutionResult(input, "Relationship node needs a source file and a target file.");
        }

        var relationshipType = node.NodeType switch
        {
            "relationship.link_note" => "hasNote",
            "relationship.link_source" => "hasSource",
            "relationship.link_export" => "hasExport",
            _ => GetParameter(node, "relationshipType", SelectedRelationshipType)
        };
        var created = await _repository.Relationships.TryCreateRelationshipAsync(source.Id, target.Id, relationshipType);
        await RefreshRelationshipsAsync();
        var status = created ? "created" : "already existed or was invalid";
        return new BrowserNodeExecutionResult(input, $"Relationship {status}: {source.FileName} -> {target.FileName} ({relationshipType}).");
    }

    private BrowserNodeExecutionResult ExecuteBrowserSafeActionNode(BrowserWorkflowNode node, List<BrowserFileRow> input)
    {
        var output = node.NodeType switch
        {
            "index.detect_duplicates" => input
                .GroupBy(row => row.Record.FileHash, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .SelectMany(group => group)
                .ToList(),
            _ => input.ToList()
        };

        var label = node.NodeType.StartsWith("file.", StringComparison.Ordinal) || node.NodeType.StartsWith("create.", StringComparison.Ordinal)
            ? $"{node.Title} is simulated in Browser Demo because direct desktop file operations are unavailable."
            : $"{node.Title} completed in Browser Demo using in-memory sample data.";
        return new BrowserNodeExecutionResult(output, $"{output.Count} files. {label}");
    }

    private async Task<BrowserNodeExecutionResult> ExecuteOutputNodeAsync(BrowserWorkflowNode node, List<BrowserFileRow> input)
    {
        if (node.NodeType is "output.export_csv" or "output.export_json" or "output.export_dublin_core")
        {
            var format = node.NodeType switch
            {
                "output.export_json" => ExportFormat.Json,
                "output.export_dublin_core" => ExportFormat.DublinCoreXml,
                _ => ExportFormat.Csv
            };
            var result = await _exportService.ExportAsync(new ExportRequest
            {
                Format = format,
                Files = input.Select(row => row.Record).ToList(),
                RequestedFileName = GetParameter(node, "fileName", $"archiveflow-workflow-{DateTime.UtcNow:yyyyMMddHHmmss}")
            });

            ExportPreview = _dataStore.LastExportContent;
            await RefreshJobsAsync();
            return new BrowserNodeExecutionResult(input, $"{result.Format} export generated for {input.Count} files.");
        }

        var suffix = node.NodeType == "output.smart_collection"
            ? $" Smart collection \"{GetParameter(node, "collectionName", "New Smart Collection")}\" simulated."
            : string.Empty;
        return new BrowserNodeExecutionResult(input, $"{input.Count} files displayed in {node.Title}.{suffix}");
    }

    private static bool IsSourceNode(BrowserWorkflowNode node)
    {
        return node.Kind == "Sources";
    }

    private static bool IsMissingDescriptiveMetadata(BrowserFileRow row)
    {
        return !row.MetadataSummary.Contains("Title:", StringComparison.OrdinalIgnoreCase) ||
               !row.MetadataSummary.Contains("Subject:", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<BrowserFileRow> FilterByExtension(IEnumerable<BrowserFileRow> input, string extension)
    {
        if (string.IsNullOrWhiteSpace(extension) || extension.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            return input;
        }

        var normalized = extension.Trim();
        if (!normalized.StartsWith(".", StringComparison.Ordinal))
        {
            normalized = "." + normalized;
        }

        return input.Where(row => string.Equals(row.Extension, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<BrowserFileRow> FilterByText(IEnumerable<BrowserFileRow> input, string query, Func<BrowserFileRow, string> selector)
    {
        return string.IsNullOrWhiteSpace(query)
            ? input
            : input.Where(row => selector(row).Contains(query.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesFileType(BrowserFileRow row, string fileType)
    {
        return fileType switch
        {
            "Document" => row.Extension is ".pdf" or ".md" or ".docx" or ".txt" or ".bib",
            "Image" => row.Extension is ".png" or ".jpg" or ".jpeg" or ".svg",
            "Video" => row.Extension is ".mp4" or ".mov",
            "Audio" => row.Extension is ".mp3" or ".wav",
            "Code" => row.Extension is ".cs" or ".json" or ".xml" or ".yaml",
            "3D Model" => row.Extension is ".glb" or ".gltf" or ".obj",
            "Archive" => row.Extension is ".zip" or ".7z",
            _ => true
        };
    }

    private static bool MatchesDateRange(BrowserFileRow row, string startDate, string endDate)
    {
        var importedAt = row.Record.ImportedAt.Date;
        if (DateTime.TryParse(startDate, out var start) && importedAt < start.Date)
        {
            return false;
        }

        return !DateTime.TryParse(endDate, out var end) || importedAt <= end.Date;
    }

    private static bool MatchesSizeRange(BrowserFileRow row, string minSizeMb, string maxSizeMb)
    {
        var minBytes = ParseDouble(minSizeMb, 0) * 1024 * 1024;
        var maxBytes = ParseDouble(maxSizeMb, 100) * 1024 * 1024;
        return row.Record.FileSize >= minBytes && row.Record.FileSize <= maxBytes;
    }

    private static bool MatchesStatus(BrowserFileRow row, string status)
    {
        return string.IsNullOrWhiteSpace(status) ||
               row.Record.GetStatus().ToString().Equals(status, StringComparison.OrdinalIgnoreCase) ||
               HasMetadata(row, "status", status);
    }

    private static bool HasMetadata(BrowserFileRow row, string fieldName, string value)
    {
        return string.IsNullOrWhiteSpace(value) ||
               row.MetadataValues.Any(item =>
                   string.Equals(item.FieldName, fieldName, StringComparison.OrdinalIgnoreCase) &&
                   item.ValueText.Contains(value.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesMetadataRule(BrowserFileRow row, string fieldName, string operatorName, string value)
    {
        var values = row.MetadataValues
            .Where(item => string.Equals(item.FieldName, fieldName, StringComparison.OrdinalIgnoreCase))
            .Select(item => item.ValueText)
            .ToList();

        return operatorName switch
        {
            "is empty" => values.Count == 0 || values.All(string.IsNullOrWhiteSpace),
            "is not empty" => values.Any(item => !string.IsNullOrWhiteSpace(item)),
            "equals" => values.Any(item => item.Equals(value, StringComparison.OrdinalIgnoreCase)),
            "starts with" => values.Any(item => item.StartsWith(value, StringComparison.OrdinalIgnoreCase)),
            "ends with" => values.Any(item => item.EndsWith(value, StringComparison.OrdinalIgnoreCase)),
            _ => values.Any(item => string.IsNullOrWhiteSpace(value) || item.Contains(value, StringComparison.OrdinalIgnoreCase))
        };
    }

    private static List<BrowserFileRow> ExecuteFullTextSearch(IEnumerable<BrowserFileRow> input, string query, string mode)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            query = "metadata archive";
        }

        if (mode == "Phrase")
        {
            return input.Where(row => row.Preview.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var terms = query.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return mode == "Any term"
            ? input.Where(row => terms.Any(term => row.Preview.Contains(term, StringComparison.OrdinalIgnoreCase))).ToList()
            : input.Where(row => terms.All(term => row.Preview.Contains(term, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    private static List<BrowserFileRow> ExecuteBooleanSearch(IEnumerable<BrowserFileRow> input, string query)
    {
        var normalized = query.Trim();
        if (normalized.Contains(" OR ", StringComparison.OrdinalIgnoreCase))
        {
            var terms = normalized.Split(" OR ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return input.Where(row => terms.Any(term => BuildSearchText(row).Contains(term, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        var andTerms = normalized.Contains(" AND ", StringComparison.OrdinalIgnoreCase)
            ? normalized.Split(" AND ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : normalized.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return input.Where(row => andTerms.All(term => BuildSearchText(row).Contains(term, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    private static string BuildSearchText(BrowserFileRow row)
    {
        return string.Join(' ', row.FileName, row.Path, row.Extension, row.Preview, row.MetadataSummary);
    }

    private static List<BrowserFileRow> IntersectInputs(IReadOnlyList<List<BrowserFileRow>> inputSets)
    {
        if (inputSets.Count == 0)
        {
            return [];
        }

        var ids = inputSets.First().Select(row => row.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var rows in inputSets.Skip(1))
        {
            ids.IntersectWith(rows.Select(row => row.Id));
        }

        return inputSets.SelectMany(rows => rows).DistinctBy(row => row.Id).Where(row => ids.Contains(row.Id)).ToList();
    }

    private static List<BrowserFileRow> DifferenceInputs(IReadOnlyList<List<BrowserFileRow>> inputSets)
    {
        if (inputSets.Count == 0)
        {
            return [];
        }

        var excluded = inputSets.Skip(1).SelectMany(rows => rows).Select(row => row.Id).ToHashSet(StringComparer.Ordinal);
        return inputSets.First().Where(row => !excluded.Contains(row.Id)).ToList();
    }

    private static IEnumerable<BrowserFileRow> SortRows(IEnumerable<BrowserFileRow> input, string field, string direction)
    {
        Func<BrowserFileRow, object> keySelector = field switch
        {
            "Modified Date" => row => row.Record.ModifiedAt ?? row.Record.ImportedAt,
            "Filename" => row => row.FileName,
            "Size" => row => row.Record.FileSize,
            "Status" => row => row.Record.GetStatus().ToString(),
            _ => row => row.Record.ImportedAt
        };

        return direction == "Ascending"
            ? input.OrderBy(keySelector)
            : input.OrderByDescending(keySelector);
    }

    private static int ParseInt(string value, int fallback)
    {
        return int.TryParse(value, out var parsed) ? Math.Max(0, parsed) : fallback;
    }

    private static double ParseDouble(string value, double fallback)
    {
        return double.TryParse(value, out var parsed) ? Math.Max(0, parsed) : fallback;
    }

    private static string GetParameter(BrowserWorkflowNode node, string key, string fallback = "")
    {
        return node.Parameters.FirstOrDefault(parameter => string.Equals(parameter.Key, key, StringComparison.OrdinalIgnoreCase))?.Value?.Trim() ?? fallback;
    }

    private static string GetSearchQuery(BrowserWorkflowNode node)
    {
        return node.NodeType switch
        {
            "search.boolean" => GetParameter(node, "expression", "AI AND metadata"),
            "search.filename" => GetParameter(node, "filename"),
            "search.content" => GetParameter(node, "contentQuery"),
            "search.full_text" => GetParameter(node, "query"),
            _ => GetParameter(node, "query")
        };
    }

    private async Task RefreshRelationshipsAsync()
    {
        Relationships.Clear();
        foreach (var relationship in await _repository.Relationships.GetAllRelationshipsAsync())
        {
            Relationships.Add(relationship);
        }

        RelationshipCount = Relationships.Count;
        UpdateWorkflowCounts();
    }

    private async Task RefreshJobsAsync()
    {
        ExportJobs.Clear();
        foreach (var job in await _repository.ExportJobs.GetRecentAsync(20))
        {
            ExportJobs.Add(job);
        }

        ImportJobs.Clear();
        foreach (var job in await _repository.ImportJobs.GetRecentAsync(20))
        {
            ImportJobs.Add(job);
        }
    }

    private void BuildWorkspaceDemo()
    {
        _nextNodeNumber = 1;
        _nextGroupNumber = 1;
        _pendingConnectionSource = null;
        ConnectionStatus = "Click a node output dot, then click a target input dot to connect.";
        NodeLibraryGroups.Clear();
        var definitions = BuiltInNodeDefinitions.CreateAll();
        foreach (var group in definitions.GroupBy(definition => definition.Subcategory))
        {
            NodeLibraryGroups.Add(new BrowserNodeLibraryGroup(
                group.Key,
                group.Select(CreateLibraryItem).ToList()));
        }

        WorkflowNodes.Clear();
        WorkflowGroups.Clear();

        AddStarterNode(definitions, "source.all_files", "source-all", 40, 110);
        AddStarterNode(definitions, "filter.extension", "filter-extension", 210, 110);
        AddStarterNode(definitions, "search.keyword", "search-keyword", 380, 110);
        AddStarterNode(definitions, "metadata.add_tag", "metadata-action", 550, 110);
        AddStarterNode(definitions, "output.result_table", "output-result", 720, 110);

        SetNodeParameter("filter-extension", "extension", ".pdf");
        SetNodeParameter("search-keyword", "query", "");
        SetNodeParameter("metadata-action", "tag", "browser-demo");

        WorkflowEdges.Clear();
        AddEdge(WorkflowNodes[0], WorkflowNodes[1]);
        AddEdge(WorkflowNodes[1], WorkflowNodes[2]);
        AddEdge(WorkflowNodes[2], WorkflowNodes[3]);
        AddEdge(WorkflowNodes[3], WorkflowNodes[4]);

        SelectedWorkflowNode = WorkflowNodes.FirstOrDefault();
        RecalculateEdges();
    }

    private void AddStarterNode(IReadOnlyList<NodeDefinition> definitions, string nodeType, string id, double x, double y)
    {
        var definition = definitions.First(item => item.NodeType == nodeType);
        WorkflowNodes.Add(CreateLibraryItem(definition).CreateNode(id, x, y));
    }

    private static BrowserNodeLibraryItem CreateLibraryItem(NodeDefinition definition)
    {
        return BrowserNodeLibraryItem.FromDefinition(definition);
    }

    private void SetNodeParameter(string nodeId, string key, string value)
    {
        var node = WorkflowNodes.FirstOrDefault(item => item.Id == nodeId);
        var parameter = node?.Parameters.FirstOrDefault(item => item.Key == key);
        if (parameter is not null)
        {
            parameter.Value = value;
        }
    }

    private bool AddEdge(BrowserWorkflowNode source, BrowserWorkflowNode target)
    {
        if (!source.CanStartOutput || !target.CanAcceptInput || ReferenceEquals(source, target))
        {
            return false;
        }

        if (WorkflowEdges.Any(edge => ReferenceEquals(edge.Source, source) && ReferenceEquals(edge.Target, target)))
        {
            return false;
        }

        WorkflowEdges.Add(new BrowserWorkflowEdge(source, target));
        RecalculateEdges();
        return true;
    }

    private void ResizeSelectedNode(double widthDelta, double heightDelta)
    {
        if (SelectedWorkflowNode is null)
        {
            StatusMessage = "Select a node to resize.";
            return;
        }

        SelectedWorkflowNode.Width = Math.Max(120, Math.Min(240, SelectedWorkflowNode.Width + widthDelta));
        SelectedWorkflowNode.Height = Math.Max(88, Math.Min(170, SelectedWorkflowNode.Height + heightDelta));
        foreach (var group in WorkflowGroups.Where(group => group.Contains(SelectedWorkflowNode)))
        {
            group.Recalculate();
        }

        RecalculateEdges();
        StatusMessage = $"Resized node: {SelectedWorkflowNode.Title}.";
    }

    private void RecalculateEdges()
    {
        foreach (var edge in WorkflowEdges)
        {
            edge.Recalculate();
        }
    }

    private void UpdateWorkflowCounts()
    {
        if (WorkflowNodes.Count == 0)
        {
            return;
        }

        var allCount = Files.Count;
        var filteredCount = FilteredFiles.Count;
        SetOutput("source-all", $"{allCount} files loaded from browser demo data.");
        var extension = WorkflowNodes.FirstOrDefault(item => item.Id == "filter-extension") is { } extensionNode
            ? GetParameter(extensionNode, "extension", ".pdf")
            : SelectedExtensionFilter;
        SetOutput("filter-extension", extension == "All"
            ? $"{allCount} files pass because extension filter is All."
            : $"{filteredCount} files match {extension}.");
        var query = WorkflowNodes.FirstOrDefault(item => item.Id == "search-keyword") is { } searchNode
            ? GetParameter(searchNode, "query")
            : SearchText;
        SetOutput("search-keyword", string.IsNullOrWhiteSpace(query)
            ? $"{filteredCount} files pass because no query is set."
            : $"{filteredCount} files match \"{query.Trim()}\".");
        SetOutput("metadata-action", PendingChanges.Count == 0
            ? "Run workflow to preview metadata tag changes."
            : $"{PendingChanges.Count} pending metadata tag changes.");
        SetOutput("output-result", $"{filteredCount} files visible in the result table.");
        OnSelectedWorkflowNodeChanged(SelectedWorkflowNode);
    }

    private void SetOutput(string nodeId, string value)
    {
        var node = WorkflowNodes.FirstOrDefault(item => item.Id == nodeId);
        if (node is not null)
        {
            node.OutputLabel = value;
        }
    }
}

public sealed class BrowserNodeLibraryGroup(string name, IReadOnlyList<BrowserNodeLibraryItem> nodes)
{
    public string Name { get; } = name;

    public IReadOnlyList<BrowserNodeLibraryItem> Nodes { get; } = nodes;
}

public sealed class BrowserNodeLibraryItem(
    string nodeType,
    string kind,
    string icon,
    string title,
    string description,
    string color,
    string outputLabel,
    bool canAcceptInput,
    bool canStartOutput,
    IReadOnlyList<BrowserNodeParameter> parameters)
{
    public string NodeType { get; } = nodeType;

    public string Kind { get; } = kind;

    public string Icon { get; } = icon;

    public string Title { get; } = title;

    public string Description { get; } = description;

    public string Color { get; } = color;

    public string OutputLabel { get; } = outputLabel;

    public IReadOnlyList<BrowserNodeParameter> Parameters { get; } = parameters;

    public bool CanAcceptInput { get; } = canAcceptInput;

    public bool CanStartOutput { get; } = canStartOutput;

    public static BrowserNodeLibraryItem FromDefinition(NodeDefinition definition)
    {
        var parameters = definition.Parameters.Select(BrowserNodeParameter.FromDefinition).ToList();
        return new BrowserNodeLibraryItem(
            definition.NodeType,
            definition.Subcategory,
            definition.Icon,
            definition.DisplayName,
            definition.Description,
            definition.AccentColor,
            definition.IsActionNode
                ? "Action runs in preview/apply mode."
                : "Ready to execute in Browser Demo.",
            definition.InputPorts.Count > 0,
            definition.OutputPorts.Count > 0,
            parameters);
    }

    public BrowserWorkflowNode CreateNode(string id, double x, double y)
    {
        return new BrowserWorkflowNode(
            id,
            NodeType,
            Title,
            Kind,
            Icon,
            Description,
            OutputLabel,
            x,
            y,
            Color,
            CanAcceptInput,
            CanStartOutput,
            Parameters.Select(parameter => parameter.Clone()).ToList());
    }
}

public sealed partial class BrowserWorkflowNode : ObservableObject
{
    public BrowserWorkflowNode(
        string id,
        string nodeType,
        string title,
        string kind,
        string icon,
        string description,
        string outputLabel,
        double x,
        double y,
        string color,
        bool canAcceptInput,
        bool canStartOutput,
        IReadOnlyList<BrowserNodeParameter> parameters)
    {
        Id = id;
        NodeType = nodeType;
        Title = title;
        Kind = kind;
        Icon = icon;
        Description = description;
        OutputLabel = outputLabel;
        X = x;
        Y = y;
        Color = color;
        CanAcceptInput = canAcceptInput;
        CanStartOutput = canStartOutput;
        Parameters = new ObservableCollection<BrowserNodeParameter>(parameters.Select(parameter => parameter.Clone()));
    }

    public string Id { get; }

    public string NodeType { get; }

    public string Title { get; }

    public string Kind { get; }

    public string Icon { get; }

    public string Description { get; }

    public string Color { get; }

    public bool CanAcceptInput { get; }

    public bool CanStartOutput { get; }

    public ObservableCollection<BrowserNodeParameter> Parameters { get; }

    public BrowserWorkflowNode Clone(string id, string title, double x, double y)
    {
        return new BrowserWorkflowNode(
            id,
            NodeType,
            title,
            Kind,
            Icon,
            Description,
            OutputLabel,
            x,
            y,
            Color,
            CanAcceptInput,
            CanStartOutput,
            Parameters.Select(parameter => parameter.Clone()).ToList());
    }

    [ObservableProperty]
    private double _width = 145;

    [ObservableProperty]
    private double _height = 108;

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _outputLabel = string.Empty;
}

public sealed partial class BrowserNodeParameter(
    string key,
    string name,
    NodeParameterControlType controlType,
    string value,
    IReadOnlyList<string> options) : ObservableObject
{
    public string Key { get; } = key;

    public string Name { get; } = name;

    public NodeParameterControlType ControlType { get; } = controlType;

    [ObservableProperty]
    private string _value = value;

    public IReadOnlyList<string> Options { get; } = options;

    public bool IsText => ControlType == NodeParameterControlType.Text;

    public bool IsNumber => ControlType == NodeParameterControlType.Number;

    public bool IsDropdown => ControlType == NodeParameterControlType.Dropdown;

    public bool IsDate => ControlType == NodeParameterControlType.Date;

    public bool IsCheckbox => ControlType == NodeParameterControlType.Boolean;

    public static BrowserNodeParameter FromDefinition(NodeParameterDefinition definition)
    {
        return new BrowserNodeParameter(
            definition.Key,
            definition.DisplayName,
            definition.ControlType,
            definition.DefaultValue,
            definition.Options);
    }

    public BrowserNodeParameter Clone()
    {
        return new BrowserNodeParameter(Key, Name, ControlType, Value, Options.ToList());
    }
}

public sealed partial class BrowserWorkflowEdge : ObservableObject
{
    public BrowserWorkflowEdge(BrowserWorkflowNode source, BrowserWorkflowNode target)
    {
        Source = source;
        Target = target;
        Recalculate();
    }

    public BrowserWorkflowNode Source { get; }

    public BrowserWorkflowNode Target { get; }

    [ObservableProperty]
    private string _pathData = string.Empty;

    [ObservableProperty]
    private bool _isSelected;

    public void Recalculate()
    {
        var startX = Source.X + Source.Width;
        var startY = Source.Y + Source.Height / 2;
        var endX = Target.X;
        var endY = Target.Y + Target.Height / 2;
        var controlOffset = Math.Max(80, (endX - startX) / 2);
        PathData = $"M {startX:0.##},{startY:0.##} C {startX + controlOffset:0.##},{startY:0.##} {endX - controlOffset:0.##},{endY:0.##} {endX:0.##},{endY:0.##}";
    }
}

public sealed partial class BrowserWorkflowGroup : ObservableObject
{
    private readonly IReadOnlyList<BrowserWorkflowNode> _nodes;

    private BrowserWorkflowGroup(string id, string name, IReadOnlyList<BrowserWorkflowNode> nodes)
    {
        Id = id;
        Name = name;
        _nodes = nodes;
        Recalculate();
    }

    public string Id { get; }

    public string Name { get; }

    public static BrowserWorkflowGroup FromNodes(string id, string name, IReadOnlyList<BrowserWorkflowNode> nodes)
    {
        return new BrowserWorkflowGroup(id, name, nodes);
    }

    public bool Contains(BrowserWorkflowNode node)
    {
        return _nodes.Any(item => ReferenceEquals(item, node));
    }

    public void Recalculate()
    {
        if (_nodes.Count == 0)
        {
            return;
        }

        var left = _nodes.Min(node => node.X) - 22;
        var top = _nodes.Min(node => node.Y) - 34;
        var right = _nodes.Max(node => node.X + node.Width) + 22;
        var bottom = _nodes.Max(node => node.Y + node.Height) + 22;

        X = Math.Max(0, left);
        Y = Math.Max(0, top);
        Width = Math.Max(160, right - X);
        Height = Math.Max(120, bottom - Y);
    }

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private double _width;

    [ObservableProperty]
    private double _height;
}

public sealed class BrowserNodeExecutionResult(IReadOnlyList<BrowserFileRow> files, string outputLabel)
{
    public List<BrowserFileRow> Files { get; } = files.ToList();

    public string OutputLabel { get; } = outputLabel;
}

public sealed class BrowserPendingChange(string fileId, string fileName, string action, string value)
{
    public string FileId { get; } = fileId;

    public string FileName { get; } = fileName;

    public string Action { get; } = action;

    public string Value { get; } = value;
}

public sealed class BrowserFileRow
{
    public BrowserFileRow(FileRecord record, IReadOnlyList<MetadataValue> metadata)
    {
        Record = record;
        Id = record.Id;
        ArchiveId = record.ArchiveId;
        FileName = record.FileName;
        Extension = record.FileExtension;
        Path = record.FilePath;
        Size = $"{Math.Max(1, record.FileSize / 1024):N0} KB";
        Preview = record.ContentPreview;
        MetadataValues = metadata;
        MetadataSummary = metadata.Count == 0
            ? "No metadata"
            : string.Join(", ", metadata.Select(item => $"{item.DisplayName}: {item.ValueText}"));
    }

    public FileRecord Record { get; }

    public string Id { get; }

    public string ArchiveId { get; }

    public string FileName { get; }

    public string Extension { get; }

    public string Path { get; }

    public string Size { get; }

    public string Preview { get; }

    public IReadOnlyList<MetadataValue> MetadataValues { get; }

    public string MetadataSummary { get; }

    public override string ToString()
    {
        return FileName;
    }
}
