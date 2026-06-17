using System.Runtime.CompilerServices;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Infrastructure.FileSystem;

public class LocalFileScanner : IFileScanner
{
    private readonly IFileHashingService _hashingService;
    private readonly ILogger<LocalFileScanner> _logger;

    public LocalFileScanner(IFileHashingService hashingService, ILogger<LocalFileScanner> logger)
    {
        _hashingService = hashingService;
        _logger = logger;
    }

    public async IAsyncEnumerable<FileRecord> ScanDirectoryAsync(
        string directoryPath, 
        bool recursive = true, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directoryPath))
        {
            _logger.LogWarning("Directory does not exist: {DirectoryPath}", directoryPath);
            yield break;
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.EnumerateFiles(directoryPath, "*", searchOption);

        foreach (var filePath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileInfo = new FileInfo(filePath);
            
            // Skip hidden and system files
            if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden) || 
                fileInfo.Attributes.HasFlag(FileAttributes.System))
            {
                continue;
            }

            FileRecord? record = null;

            try
            {
                _logger.LogDebug("Scanning file: {FilePath}", filePath);

                var hash = await _hashingService.ComputeSha256HashAsync(filePath, cancellationToken);
                var mimeType = GetSimpleMimeType(fileInfo.Extension);

                record = FileRecord.Create(filePath, hash, fileInfo.Length, mimeType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scan file: {FilePath}", filePath);
            }

            // Yield the record outside the try-catch block
            if (record != null)
            {
                yield return record;
            }
        }
    }

    private string GetSimpleMimeType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".mp4" => "video/mp4",
            ".md" => "text/markdown",
            _ => "application/octet-stream"
        };
    }
}
