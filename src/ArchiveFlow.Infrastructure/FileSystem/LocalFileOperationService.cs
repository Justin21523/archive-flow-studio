using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Infrastructure.FileSystem;

/// <summary>
/// Implements physical file operations with safety checks and database synchronization.
/// </summary>
public class LocalFileOperationService : IFileOperationService
{
    private readonly IFileRepository _fileRepository;
    private readonly ILogger<LocalFileOperationService> _logger;

    public LocalFileOperationService(IFileRepository fileRepository, ILogger<LocalFileOperationService> logger)
    {
        _fileRepository = fileRepository;
        _logger = logger;
    }

    public Task<IEnumerable<FileOperationPreview>> PreviewRenameAsync(IEnumerable<FileRecord> files, string prefix, string suffix)
    {
        var previews = new List<FileOperationPreview>();

        foreach (var file in files)
        {
            if (!File.Exists(file.FilePath))
            {
                previews.Add(new FileOperationPreview 
                { 
                    FileName = file.FileName, 
                    OriginalPath = file.FilePath, 
                    IsValid = false, 
                    ErrorMessage = "File not found on disk." 
                });
                continue;
            }

            var dir = Path.GetDirectoryName(file.FilePath);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(file.FileName);
            var ext = Path.GetExtension(file.FileName);
            var newName = $"{prefix}{nameWithoutExt}{suffix}{ext}";
            var newPath = Path.Combine(dir!, newName);

            previews.Add(new FileOperationPreview
            {
                FileName = file.FileName,
                OriginalPath = file.FilePath,
                NewPath = newPath,
                IsValid = !File.Exists(newPath) // Check for collision
            });
        }

        return Task.FromResult<IEnumerable<FileOperationPreview>>(previews);
    }

    public async Task ExecuteRenameAsync(IEnumerable<FileRecord> files, string prefix, string suffix)
    {
        foreach (var file in files.ToList())
        {
            if (!File.Exists(file.FilePath)) continue;

            var dir = Path.GetDirectoryName(file.FilePath);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(file.FileName);
            var ext = Path.GetExtension(file.FileName);
            var newName = $"{prefix}{nameWithoutExt}{suffix}{ext}";
            var newPath = Path.Combine(dir!, newName);

            if (File.Exists(newPath))
            {
                _logger.LogWarning("File rename skipped due to collision: {NewPath}", newPath);
                continue;
            }

            // 1. Move physical file
            File.Move(file.FilePath, newPath);
            _logger.LogInformation("Renamed file: {Old} -> {New}", file.FilePath, newPath);

            // 2. Update database record
            file.UpdatePath(newPath);
            await _fileRepository.SaveAsync(file);
        }
    }

    public Task<IEnumerable<FileOperationPreview>> PreviewMoveAsync(IEnumerable<FileRecord> files, string targetDirectory)
    {
        var previews = new List<FileOperationPreview>();

        if (!Directory.Exists(targetDirectory))
        {
            // If directory doesn't exist, we can't preview accurately, but we allow it for creation
        }

        foreach (var file in files)
        {
            if (!File.Exists(file.FilePath))
            {
                previews.Add(new FileOperationPreview 
                { 
                    FileName = file.FileName, 
                    OriginalPath = file.FilePath, 
                    IsValid = false, 
                    ErrorMessage = "File not found on disk." 
                });
                continue;
            }

            var newPath = Path.Combine(targetDirectory, file.FileName);
            previews.Add(new FileOperationPreview
            {
                FileName = file.FileName,
                OriginalPath = file.FilePath,
                NewPath = newPath,
                IsValid = !File.Exists(newPath)
            });
        }

        return Task.FromResult<IEnumerable<FileOperationPreview>>(previews);
    }

    public async Task ExecuteMoveAsync(IEnumerable<FileRecord> files, string targetDirectory)
    {
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
            _logger.LogInformation("Created target directory: {Dir}", targetDirectory);
        }

        foreach (var file in files.ToList())
        {
            if (!File.Exists(file.FilePath)) continue;

            var newPath = Path.Combine(targetDirectory, file.FileName);

            if (File.Exists(newPath))
            {
                _logger.LogWarning("File move skipped due to collision: {NewPath}", newPath);
                continue;
            }

            // 1. Move physical file
            File.Move(file.FilePath, newPath);
            _logger.LogInformation("Moved file: {Old} -> {New}", file.FilePath, newPath);

            // 2. Update database record
            file.UpdatePath(newPath);
            await _fileRepository.SaveAsync(file);
        }
    }
}