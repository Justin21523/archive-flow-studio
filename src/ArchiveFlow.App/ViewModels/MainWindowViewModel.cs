using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IFileScanner _fileScanner;
    private readonly IFileRepository _fileRepository;
    private readonly ILogger<MainWindowViewModel> _logger;

    [ObservableProperty]
    private string _selectedPath = string.Empty;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusMessage = "Ready.";

    public ObservableCollection<FileRecord> Files { get; } = new();

    public MainWindowViewModel(
        IFileScanner fileScanner, 
        IFileRepository fileRepository, 
        ILogger<MainWindowViewModel> logger)
    {
        _fileScanner = fileScanner;
        _fileRepository = fileRepository;
        _logger = logger;
    }

    // Removed async keyword since there's no await
    [RelayCommand]
    private void SelectFolder()
    {
        SelectedPath = "/tmp/archiveflow_test";
        
        if (!System.IO.Directory.Exists(SelectedPath))
        {
            System.IO.Directory.CreateDirectory(SelectedPath);
            System.IO.File.WriteAllText(System.IO.Path.Combine(SelectedPath, "test1.txt"), "Hello ArchiveFlow");
            System.IO.File.WriteAllText(System.IO.Path.Combine(SelectedPath, "test2.md"), "# Test Markdown");
            _logger.LogInformation("Created test folder with sample files at: {Path}", SelectedPath);
        }
        
        StatusMessage = $"Selected folder: {SelectedPath}";
    }

    [RelayCommand]
    private async Task ScanFilesAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedPath) || !System.IO.Directory.Exists(SelectedPath))
        {
            StatusMessage = "Please select a valid folder first.";
            return;
        }

        IsScanning = true;
        StatusMessage = "Scanning...";
        Files.Clear();

        try
        {
            int count = 0;
            await foreach (var record in _fileScanner.ScanDirectoryAsync(SelectedPath))
            {
                var existing = await _fileRepository.GetByHashAsync(record.FileHash);
                if (existing != null)
                {
                    _logger.LogWarning("Duplicate file found: {Path}", record.FilePath);
                    continue;
                }

                await _fileRepository.SaveAsync(record);
                Files.Add(record);
                count++;
                StatusMessage = $"Scanned {count} files...";
            }

            StatusMessage = $"Scan complete. Added {count} new files to the archive.";
            _logger.LogInformation("Scan completed. Added {Count} files.", count);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error during scanning.");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }
}
