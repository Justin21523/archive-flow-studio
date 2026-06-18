using System;
using System.Linq;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.App.ViewModels;

public partial class DatabaseManagerViewModel : ObservableObject
{
    private readonly IFileRepository _fileRepository;
    private readonly IMetadataRepository _metadataRepository;
    private readonly ISearchService _searchService;
    private readonly ILogger<DatabaseManagerViewModel> _logger;

    [ObservableProperty] private int _totalFiles;
    [ObservableProperty] private string _totalSize = "0 MB";
    [ObservableProperty] private int _totalMetadata;
    [ObservableProperty] private string _statusMessage = "Ready.";

    public DatabaseManagerViewModel(
        IFileRepository fileRepository,
        IMetadataRepository metadataRepository,
        ISearchService searchService,
        ILogger<DatabaseManagerViewModel> logger)
    {
        _fileRepository = fileRepository;
        _metadataRepository = metadataRepository;
        _searchService = searchService;
        _logger = logger;
        
        RefreshStatsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task RefreshStatsAsync()
    {
        try
        {
            var files = await _fileRepository.GetAllAsync();
            TotalFiles = files.Count();
            long sizeBytes = files.Sum(f => f.FileSize);
            TotalSize = sizeBytes > 1_000_000 ? $"{sizeBytes / 1_000_000} MB" : $"{sizeBytes / 1_000} KB";
            
            // Mock metadata count (in real app, query DB)
            TotalMetadata = TotalFiles * 2; 
            StatusMessage = "Statistics refreshed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RebuildIndexAsync()
    {
        StatusMessage = "Rebuilding index... (This may take a while)";
        var files = await _fileRepository.GetAllAsync();
        foreach(var file in files) await _searchService.IndexFileAsync(file);
        StatusMessage = "Index rebuilt successfully.";
    }

    [RelayCommand]
    private void ClearMetadataCommand()
    {
        StatusMessage = "Metadata cleared. (Mock action)";
    }

    [RelayCommand]
    private void DeleteAllCommand()
    {
        StatusMessage = "All files deleted. (Mock action - DANGER)";
    }
}