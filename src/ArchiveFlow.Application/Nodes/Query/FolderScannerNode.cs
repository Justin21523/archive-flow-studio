using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// Scans a specific folder and returns all files within it.
/// </summary>
public class FolderScannerNode : IArchiveNode
{
    private readonly IFileRepository _fileRepository;
    private readonly ISearchService _searchService;
    private readonly IFilePreviewService _previewService;
    public string FolderPath { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Scan Folder: {FolderPath}";
    public double X { get; set; }
    public double Y { get; set; }

    public FolderScannerNode(IFileRepository fileRepository, ISearchService searchService, IFilePreviewService previewService)
    {
        _fileRepository = fileRepository;
        _searchService = searchService;
        _previewService = previewService;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        var allFiles = await _fileRepository.GetAllAsync(cancellationToken);
        
        // Filter files by folder path if specified
        IEnumerable<FileRecord> filteredFiles = allFiles;
        if (!string.IsNullOrWhiteSpace(FolderPath))
        {
            filteredFiles = allFiles.Where(f => f.FilePath.StartsWith(FolderPath, StringComparison.OrdinalIgnoreCase));
        }

        var fileList = new List<FileRecord>();
        foreach (var file in filteredFiles)
        {
            await _searchService.IndexFileAsync(file);
            if (string.IsNullOrEmpty(file.ThumbnailPath) && string.IsNullOrEmpty(file.ContentPreview))
            {
                await _previewService.GeneratePreviewAsync(file);
            }
            fileList.Add(file);
        }

        context.SetFileSet(fileList);
    }
}