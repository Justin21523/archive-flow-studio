using System.Diagnostics;
using System.Runtime.InteropServices;
using ArchiveFlow.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Infrastructure.FileSystem;

/// <summary>
/// Opens files or reveals folders using the operating system shell.
/// This service is intentionally read-only and never modifies physical files.
/// </summary>
public sealed class DesktopFileSystemInteractionService : IFileSystemInteractionService
{
    private readonly ILogger<DesktopFileSystemInteractionService> _logger;

    public DesktopFileSystemInteractionService(ILogger<DesktopFileSystemInteractionService> logger)
    {
        _logger = logger;
    }

    public Task OpenFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is empty.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File does not exist.", filePath);
        }

        StartShellProcess(filePath);
        return Task.CompletedTask;
    }

    public Task RevealInFileExplorerAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is empty.", nameof(filePath));
        }

        var directory = File.Exists(filePath)
            ? Path.GetDirectoryName(filePath)
            : filePath;

        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Directory does not exist: {directory}");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            StartProcess("explorer.exe", $"/select,\"{filePath}\"");
            return Task.CompletedTask;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            StartProcess("open", $"-R \"{filePath}\"");
            return Task.CompletedTask;
        }

        StartProcess("xdg-open", $"\"{directory}\"");
        return Task.CompletedTask;
    }

    private void StartShellProcess(string target)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            StartProcess("cmd", $"/c start \"\" \"{target}\"");
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            StartProcess("open", $"\"{target}\"");
            return;
        }

        StartProcess("xdg-open", $"\"{target}\"");
    }

    private void StartProcess(string fileName, string arguments)
    {
        _logger.LogInformation("Starting shell process: {FileName} {Arguments}", fileName, arguments);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process.Start(startInfo);
    }
}