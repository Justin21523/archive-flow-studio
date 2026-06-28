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
    public ObservableCollection<BrowserPendingChange> PendingChanges { get; } = [];

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
    private string _inspectorTitle = "Workspace Overview";

    [ObservableProperty]
    private string _inspectorDescription = "Select a node to inspect its parameters and preview how it affects the demo workflow.";

    [ObservableProperty]
    private string _workflowSummary = "Ready to execute the browser workspace demo.";

    [ObservableProperty]
    private string _selectedNodeKind = "Workspace";

    [ObservableProperty]
    private string _selectedNodeOutput = "No node selected.";

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

        InspectorTitle = value?.Title ?? "Workspace Overview";
        InspectorDescription = value?.Description ?? "Select a node to inspect its parameters and preview how it affects the demo workflow.";
        SelectedNodeKind = value?.Kind ?? "Workspace";
        SelectedNodeOutput = value is null
            ? $"Current result contains {FilteredFiles.Count} files. Use the demo nodes to understand source, filter, search, metadata, and output behavior."
            : value.OutputLabel;
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
            ? $"Workflow executed. {FilteredFiles.Count} files reached the result table."
            : $"Workflow executed. {FilteredFiles.Count} files reached the result table and {PendingChanges.Count} metadata changes are ready to apply.";
        StatusMessage = WorkflowSummary;
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

    public void MoveWorkflowNode(BrowserWorkflowNode node, double x, double y)
    {
        node.X = Math.Max(20, Math.Min(1420, x));
        node.Y = Math.Max(20, Math.Min(820, y));
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
        NodeLibraryGroups.Clear();
        NodeLibraryGroups.Add(new BrowserNodeLibraryGroup("Source", ["All Files", "Recent Imports", "Missing Metadata"]));
        NodeLibraryGroups.Add(new BrowserNodeLibraryGroup("Filter", ["Extension", "Metadata Field", "Status"]));
        NodeLibraryGroups.Add(new BrowserNodeLibraryGroup("Search", ["Keyword", "Full Text", "Boolean"]));
        NodeLibraryGroups.Add(new BrowserNodeLibraryGroup("Actions", ["Add Tag", "Create Relationship"]));
        NodeLibraryGroups.Add(new BrowserNodeLibraryGroup("Output", ["Result Table", "CSV Export", "Dublin Core XML"]));

        WorkflowNodes.Clear();
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
        WorkflowEdges.Add(new BrowserWorkflowEdge(WorkflowNodes[0], WorkflowNodes[1]));
        WorkflowEdges.Add(new BrowserWorkflowEdge(WorkflowNodes[1], WorkflowNodes[2]));
        WorkflowEdges.Add(new BrowserWorkflowEdge(WorkflowNodes[2], WorkflowNodes[3]));
        WorkflowEdges.Add(new BrowserWorkflowEdge(WorkflowNodes[3], WorkflowNodes[4]));

        SelectedWorkflowNode = WorkflowNodes.FirstOrDefault();
        RecalculateEdges();
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

public sealed class BrowserNodeLibraryGroup(string name, IReadOnlyList<string> nodes)
{
    public string Name { get; } = name;

    public IReadOnlyList<string> Nodes { get; } = nodes;
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

    public double Width => 145;

    public double Height => 108;

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
