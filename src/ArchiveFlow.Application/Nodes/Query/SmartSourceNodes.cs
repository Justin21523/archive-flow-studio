using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// Source node that returns files missing required metadata fields.
/// </summary>
public class MissingMetadataNode : IArchiveNode
{
    private readonly IFileRepository _fileRepository;
    private readonly IMetadataRepository _metadataRepository;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => "Missing Metadata Files";
    public double X { get; set; }
    public double Y { get; set; }

    public MissingMetadataNode(IFileRepository fileRepository, IMetadataRepository metadataRepository)
    {
        _fileRepository = fileRepository;
        _metadataRepository = metadataRepository;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        var allFiles = (await _fileRepository.GetAllAsync(cancellationToken)).ToList();
        var requiredFields = (await _metadataRepository.GetAllFieldsAsync())
            .Where(field => field.IsRequired)
            .Select(field => field.Id)
            .ToHashSet();

        var result = new List<FileRecord>();
        foreach (var file in allFiles)
        {
            var metadata = (await _metadataRepository.GetMetadataByFileIdAsync(file.Id)).ToList();
            var hasMissingRequiredField = requiredFields.Count > 0 &&
                !requiredFields.IsSubsetOf(metadata.Select(value => value.FieldId));
            var hasNoMetadata = requiredFields.Count == 0 && metadata.Count == 0;

            if (hasMissingRequiredField || hasNoMetadata)
            {
                result.Add(file);
            }
        }

        context.SetFileSet(result);
    }
}

/// <summary>
/// Source node that returns files identified as duplicates based on hash.
/// </summary>
public class DuplicateFilesNode : IArchiveNode
{
    private readonly IFileRepository _fileRepository;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => "Duplicate Files";
    public double X { get; set; }
    public double Y { get; set; }

    public DuplicateFilesNode(IFileRepository fileRepository)
    {
        _fileRepository = fileRepository;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        var allFiles = await _fileRepository.GetAllAsync(cancellationToken);
        var duplicates = allFiles
            .GroupBy(f => f.FileHash)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .ToList();
            
        context.SetFileSet(duplicates);
    }
}
