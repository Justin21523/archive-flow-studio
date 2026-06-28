using System.Collections.ObjectModel;
using ArchiveFlow.Application.DTOs;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Browser.Services;
using ArchiveFlow.Domain.Entities;
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
        ".docx"
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
    private string _connectionStatus = "Select a node, mark it as the link source, then select another node and connect.";

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
    private void ExecuteWorkflow()
    {
        ApplyFilter();
        PendingChanges.Clear();

        var actionNode = WorkflowNodes.FirstOrDefault(node => node.Id == "metadata-action");
        if (actionNode is not null)
        {
            foreach (var file in FilteredFiles.Take(5))
            {
                PendingChanges.Add(new BrowserPendingChange(
                    file.FileName,
                    "Add tag",
                    "browser-demo"));
            }
        }

        WorkflowSummary = PendingChanges.Count == 0
            ? $"Workflow executed through {WorkflowNodes.Count} nodes and {WorkflowEdges.Count} connections. {FilteredFiles.Count} files reached the result table."
            : $"Workflow executed through {WorkflowNodes.Count} nodes and {WorkflowEdges.Count} connections. {FilteredFiles.Count} files reached the result table and {PendingChanges.Count} metadata changes are ready to apply.";
        StatusMessage = WorkflowSummary;
        UpdateWorkflowCounts();
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
        var node = new BrowserWorkflowNode(
            $"demo-node-{_nextNodeNumber++}",
            item.Title,
            item.Kind,
            item.Description,
            item.OutputLabel,
            x,
            Math.Min(760, y),
            item.Color,
            item.Parameters);

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
        var copy = new BrowserWorkflowNode(
            $"demo-node-{_nextNodeNumber++}",
            $"{source.Title} Copy",
            source.Kind,
            source.Description,
            source.OutputLabel,
            Math.Min(1320, source.X + 45),
            Math.Min(760, source.Y + 145),
            source.Color,
            source.Parameters.Select(parameter => new BrowserNodeParameter(parameter.Name, parameter.Value)).ToList());

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

        _pendingConnectionSource = SelectedWorkflowNode;
        ConnectionStatus = $"Link source: {_pendingConnectionSource.Title}. Select a target node, then click Connect.";
        StatusMessage = ConnectionStatus;
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

        if (ReferenceEquals(_pendingConnectionSource, SelectedWorkflowNode))
        {
            ConnectionStatus = "A node cannot connect to itself.";
            StatusMessage = ConnectionStatus;
            return;
        }

        if (AddEdge(_pendingConnectionSource, SelectedWorkflowNode))
        {
            ConnectionStatus = $"Connected {_pendingConnectionSource.Title} -> {SelectedWorkflowNode.Title}.";
        }
        else
        {
            ConnectionStatus = "That connection already exists.";
        }

        _pendingConnectionSource = null;
        StatusMessage = ConnectionStatus;
        UpdateWorkflowCounts();
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

        var fileNames = PendingChanges.Select(change => change.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var row in Files.Where(row => fileNames.Contains(row.FileName)))
        {
            await _repository.Metadata.AddMetadataValueIfMissingAsync(
                row.Id,
                "tag",
                "Tag",
                "String",
                "Descriptive",
                "browser-demo");
        }

        PendingChanges.Clear();
        StatusMessage = "Browser demo metadata changes applied in memory.";
        await RefreshFilesAsync();
        UpdateWorkflowCounts();
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
        ConnectionStatus = "Select a node, mark it as the link source, then select another node and connect.";
        NodeLibraryGroups.Clear();
        NodeLibraryGroups.Add(new BrowserNodeLibraryGroup("Source",
        [
            CreateLibraryItem("Source", "All Files", "Loads every file from the browser demo repository.", "#3E6B8C", "Output: all demo files.", "Archive", "Browser demo sample data"),
            CreateLibraryItem("Source", "Recent Imports", "Shows files from the latest mock import job.", "#3E6B8C", "Output: recent imported files.", "Window", "Latest mock import"),
            CreateLibraryItem("Source", "Missing Metadata", "Finds files that still need descriptive metadata.", "#3E6B8C", "Output: files missing metadata.", "Completeness", "Required fields")
        ]));
        NodeLibraryGroups.Add(new BrowserNodeLibraryGroup("Filter",
        [
            CreateLibraryItem("Filter", "Extension Filter", "Keeps files matching the selected extension filter.", "#6B7D3E", "Output: matching extension files.", "Extension", "Toolbar dropdown"),
            CreateLibraryItem("Filter", "Metadata Field", "Keeps files matching a metadata field/value rule.", "#6B7D3E", "Output: metadata matches.", "Field", "Tag / Subject / Project"),
            CreateLibraryItem("Filter", "Status Filter", "Keeps files matching an archive status.", "#6B7D3E", "Output: status matches.", "Status", "Imported / Reviewed")
        ]));
        NodeLibraryGroups.Add(new BrowserNodeLibraryGroup("Search",
        [
            CreateLibraryItem("Search", "Keyword Search", "Searches filename, path, preview text, and metadata summary.", "#7A5C9E", "Output: keyword matches.", "Query", "Toolbar search field"),
            CreateLibraryItem("Search", "Full Text Search", "Demonstrates browser-safe indexed content search over sample text.", "#7A5C9E", "Output: full-text matches.", "Index", "In-memory demo index"),
            CreateLibraryItem("Search", "Boolean Search", "Demonstrates AND / OR keyword search behavior.", "#7A5C9E", "Output: boolean query matches.", "Query", "metadata AND archive")
        ]));
        NodeLibraryGroups.Add(new BrowserNodeLibraryGroup("Actions",
        [
            CreateLibraryItem("Metadata Action", "Add Tag Preview", "Creates pending metadata changes before applying them.", "#A66A3D", "Output: pending metadata changes.", "Tag", "browser-demo"),
            CreateLibraryItem("Relationship", "Create Relationship", "Creates source-to-target relationship records from selected files.", "#A66A3D", "Output: relationship preview.", "Type", "references"),
            CreateLibraryItem("Metadata Action", "Set Status", "Previews a file status update for the result set.", "#A66A3D", "Output: pending status changes.", "Status", "Reviewed")
        ]));
        NodeLibraryGroups.Add(new BrowserNodeLibraryGroup("Output",
        [
            CreateLibraryItem("Output", "Result Table", "Displays the final file set below the canvas.", "#3F7A62", "Output: result table rows.", "View", "Result files"),
            CreateLibraryItem("Output", "CSV Export", "Exports the current result as a browser download preview.", "#3F7A62", "Output: CSV export job.", "Format", "CSV"),
            CreateLibraryItem("Output", "Dublin Core XML", "Exports the current result as a Dublin Core XML preview.", "#3F7A62", "Output: XML export job.", "Format", "Dublin Core XML")
        ]));

        WorkflowNodes.Clear();
        WorkflowGroups.Clear();
        WorkflowNodes.Add(new BrowserWorkflowNode(
            "source-all",
            "All Files",
            "Source",
            "Loads every sample archive file from the in-memory Browser Demo repository.",
            "Output: all demo files.",
            40,
            110,
            "#3E6B8C",
            [new BrowserNodeParameter("Archive", "Browser demo sample data")]));
        WorkflowNodes.Add(new BrowserWorkflowNode(
            "filter-extension",
            "Extension Filter",
            "Filter",
            "Keeps only files matching the selected extension filter. Choose All to pass every file.",
            "Output updates when the extension filter changes.",
            185,
            110,
            "#6B7D3E",
            [new BrowserNodeParameter("Extension", "Bound to the toolbar extension dropdown")]));
        WorkflowNodes.Add(new BrowserWorkflowNode(
            "search-keyword",
            "Keyword Search",
            "Search",
            "Searches filename, path, preview text, and metadata summary.",
            "Output updates when the search box changes.",
            330,
            110,
            "#7A5C9E",
            [new BrowserNodeParameter("Query", "Bound to the toolbar search field")]));
        WorkflowNodes.Add(new BrowserWorkflowNode(
            "metadata-action",
            "Add Tag Preview",
            "Metadata Action",
            "Creates pending metadata changes first; Apply writes them to the in-memory demo repository.",
            "Preview-only until Apply Pending Changes is clicked.",
            475,
            110,
            "#A66A3D",
            [new BrowserNodeParameter("Tag", "browser-demo")]));
        WorkflowNodes.Add(new BrowserWorkflowNode(
            "output-result",
            "Result Table",
            "Output",
            "Displays the final file set and can export the current result.",
            "Result table receives the filtered file set.",
            620,
            110,
            "#3F7A62",
            [new BrowserNodeParameter("View", "Result files + export preview")]));

        WorkflowEdges.Clear();
        AddEdge(WorkflowNodes[0], WorkflowNodes[1]);
        AddEdge(WorkflowNodes[1], WorkflowNodes[2]);
        AddEdge(WorkflowNodes[2], WorkflowNodes[3]);
        AddEdge(WorkflowNodes[3], WorkflowNodes[4]);

        SelectedWorkflowNode = WorkflowNodes.FirstOrDefault();
        RecalculateEdges();
    }

    private BrowserNodeLibraryItem CreateLibraryItem(
        string kind,
        string title,
        string description,
        string color,
        string outputLabel,
        string parameterName,
        string parameterValue)
    {
        return new BrowserNodeLibraryItem(
            kind,
            title,
            description,
            color,
            outputLabel,
            [new BrowserNodeParameter(parameterName, parameterValue)]);
    }

    private bool AddEdge(BrowserWorkflowNode source, BrowserWorkflowNode target)
    {
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
        SetOutput("filter-extension", SelectedExtensionFilter == "All"
            ? $"{allCount} files pass because extension filter is All."
            : $"{filteredCount} files match {SelectedExtensionFilter}.");
        SetOutput("search-keyword", string.IsNullOrWhiteSpace(SearchText)
            ? $"{filteredCount} files pass because no query is set."
            : $"{filteredCount} files match \"{SearchText.Trim()}\".");
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
    string kind,
    string title,
    string description,
    string color,
    string outputLabel,
    IReadOnlyList<BrowserNodeParameter> parameters)
{
    public string Kind { get; } = kind;

    public string Title { get; } = title;

    public string Description { get; } = description;

    public string Color { get; } = color;

    public string OutputLabel { get; } = outputLabel;

    public IReadOnlyList<BrowserNodeParameter> Parameters { get; } = parameters;
}

public sealed partial class BrowserWorkflowNode : ObservableObject
{
    public BrowserWorkflowNode(
        string id,
        string title,
        string kind,
        string description,
        string outputLabel,
        double x,
        double y,
        string color,
        IReadOnlyList<BrowserNodeParameter> parameters)
    {
        Id = id;
        Title = title;
        Kind = kind;
        Description = description;
        OutputLabel = outputLabel;
        X = x;
        Y = y;
        Color = color;
        Parameters = parameters;
    }

    public string Id { get; }

    public string Title { get; }

    public string Kind { get; }

    public string Description { get; }

    public string Color { get; }

    public IReadOnlyList<BrowserNodeParameter> Parameters { get; }

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

public sealed class BrowserNodeParameter(string name, string value)
{
    public string Name { get; } = name;

    public string Value { get; } = value;
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

public sealed class BrowserPendingChange(string fileName, string action, string value)
{
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

    public string MetadataSummary { get; }

    public override string ToString()
    {
        return FileName;
    }
}
