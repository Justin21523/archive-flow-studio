using System;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Application.Nodes.Query;

public class AllFilesNode : IArchiveNode
{
    private readonly IFileRepository _fileRepository;
    private readonly ISearchService _searchService; // 新增

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => "All Files";
    public double X { get; set; }
    public double Y { get; set; }

    public AllFilesNode(IFileRepository fileRepository, ISearchService searchService) // 修改建構子
    {
        _fileRepository = fileRepository;
        _searchService = searchService;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        var allFiles = await _fileRepository.GetAllAsync(cancellationToken);
        var fileList = new System.Collections.Generic.List<ArchiveFlow.Domain.Entities.FileRecord>();
        
        foreach (var file in allFiles)
        {
            // 順便建立全文索引
            await _searchService.IndexFileAsync(file);
            fileList.Add(file);
        }
        
        context.SetFileSet(fileList);
    }
}