using System;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Application.Nodes.Query;

public class AllFilesNode : IArchiveNode
{
    private readonly IFileRepository _fileRepository;
    private readonly ISearchService _searchService; // 新增
    private readonly IFilePreviewService _previewService;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => "All Files";
    public double X { get; set; }
    public double Y { get; set; }

    public AllFilesNode(IFileRepository fileRepository, ISearchService searchService, IFilePreviewService previewService)
    {
        _fileRepository = fileRepository;
        _searchService = searchService;
        _previewService = previewService;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        var allFiles = await _fileRepository.GetAllAsync(cancellationToken);
        var fileList = new System.Collections.Generic.List<ArchiveFlow.Domain.Entities.FileRecord>();
        
        foreach (var file in allFiles)
        {
            await _searchService.IndexFileAsync(file);
            
            // 新增：產生預覽 (如果還沒有)
            if (string.IsNullOrEmpty(file.ThumbnailPath) && string.IsNullOrEmpty(file.ContentPreview))
            {
                await _previewService.GeneratePreviewAsync(file);
            }
            
            fileList.Add(file);
        }
        
        context.SetFileSet(fileList);
    }
}