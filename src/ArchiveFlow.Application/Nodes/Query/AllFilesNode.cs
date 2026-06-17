using System;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Application.Nodes.Query;

public class AllFilesNode : IArchiveNode
{
    private readonly IFileRepository _fileRepository;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => "All Files";
    public double X { get; set; }
    public double Y { get; set; }

    public AllFilesNode(IFileRepository fileRepository)
    {
        _fileRepository = fileRepository;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        var allFiles = await _fileRepository.GetAllAsync(cancellationToken);
        context.SetFileSet(allFiles);
    }
}