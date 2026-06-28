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

    public string DemoNotice =>
        "This is the Browser Demo version. Some desktop-only features such as local folder scanning, native database storage, and direct file system export are simulated for online demonstration.";

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

    [RelayCommand]
    private async Task ResetDemoDataAsync()
    {
        await _demoDataService.ResetAsync();
        ImportPreviewItems.Clear();
        ExportPreview = string.Empty;
        ImportSummary = "Demo data reset.";
        StatusMessage = "Demo data reset.";
        await RefreshAllAsync();
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
    }

    private async Task RefreshRelationshipsAsync()
    {
        Relationships.Clear();
        foreach (var relationship in await _repository.Relationships.GetAllRelationshipsAsync())
        {
            Relationships.Add(relationship);
        }

        RelationshipCount = Relationships.Count;
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
