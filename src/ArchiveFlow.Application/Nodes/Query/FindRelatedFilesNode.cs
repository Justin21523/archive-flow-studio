using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// Query node that filters the context to only include files related to a specific source file.
/// </summary>
public class FindRelatedFilesNode : IArchiveNode
{
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IFileRepository _fileRepository;
    public string SourceFileId { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Find Related: {SourceFileId}";
    public double X { get; set; }
    public double Y { get; set; }

    public FindRelatedFilesNode(IRelationshipRepository relationshipRepository, IFileRepository fileRepository, string sourceFileId)
    {
        _relationshipRepository = relationshipRepository;
        _fileRepository = fileRepository;
        SourceFileId = sourceFileId;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(SourceFileId)) return;

        var relationships = await _relationshipRepository.GetRelationshipsByFileIdAsync(SourceFileId);
        var relatedIds = relationships
            .Select(r => r.SourceFileId == SourceFileId ? r.TargetFileId : r.SourceFileId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        var allFiles = await _fileRepository.GetAllAsync(cancellationToken);
        var relatedFiles = allFiles.Where(f => relatedIds.Contains(f.Id)).ToList();

        context.SetFileSet(relatedFiles);
    }
}
