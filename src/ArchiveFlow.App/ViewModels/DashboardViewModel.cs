using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ArchiveFlow.Application.DTOs;
using ArchiveFlow.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// ViewModel for the Dashboard (Lobby) view.
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<DashboardViewModel> _logger;

    [ObservableProperty] private int _totalFiles;
    [ObservableProperty] private string _totalSize = "0 MB";
    [ObservableProperty] private double _metadataCompleteness;
    [ObservableProperty] private string _statusMessage = "Ready.";
    
    public ObservableCollection<FileTypeBar> FileTypeBars { get; } = new();

    public DashboardViewModel(IStatisticsService statisticsService, ILogger<DashboardViewModel> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
        
        // Load statistics on startup
        Task.Run(async () => await LoadStatisticsAsync());
    }

    [RelayCommand]
    private async Task LoadStatisticsAsync()
    {
        StatusMessage = "Calculating archive health...";
        try
        {
            var stats = await _statisticsService.GetArchiveStatisticsAsync();
            
            TotalFiles = stats.TotalFiles;
            TotalSize = stats.TotalSizeBytes > 1_000_000 
                ? $"{stats.TotalSizeBytes / 1_000_000.0:F1} MB" 
                : $"{stats.TotalSizeBytes / 1_000.0:F1} KB";
            
            MetadataCompleteness = stats.MetadataCompleteness;
            
            // Update Chart Bars
            FileTypeBars.Clear();
            int maxCount = stats.FileTypeDistribution.Values.Any() ? stats.FileTypeDistribution.Values.Max() : 1;
            
            foreach (var kvp in stats.FileTypeDistribution)
            {
                FileTypeBars.Add(new FileTypeBar
                {
                    Extension = kvp.Key.ToUpperInvariant(),
                    Count = kvp.Value,
                    WidthPercentage = (kvp.Value / (double)maxCount) * 100
                });
            }

            StatusMessage = $"Archive health calculated. {TotalFiles} files indexed.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load statistics");
            StatusMessage = $"Error loading statistics: {ex.Message}";
        }
    }
}