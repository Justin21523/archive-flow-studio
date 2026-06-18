using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// Filters files that have a specific tag in their metadata.
/// </summary>
public class TagFilterNode : IArchiveNode
{
    private readonly IMetadataRepository _metadataRepository;
    public string TargetTag { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Tag Filter: {TargetTag}";
    public double X { get; set; }
    public double Y { get; set; }

    public TagFilterNode(IMetadataRepository metadataRepository, string targetTag)
    {
        _metadataRepository = metadataRepository;
        TargetTag = targetTag;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(TargetTag)) return;

        var taggedFileIds = new System.Collections.Generic.HashSet<string>();
        
        // Get all files with the target tag
        foreach (var file in context.CurrentFileSet)
        {
            var metadata = await _metadataRepository.GetMetadataByFileIdAsync(file.Id);
            if (metadata.Any(m => m.FieldName.ToLowerInvariant() == "tag" && 
                                 m.ValueText?.ToLowerInvariant() == TargetTag.ToLowerInvariant()))
            {
                taggedFileIds.Add(file.Id);
            }
        }

        var filtered = context.CurrentFileSet.Where(f => taggedFileIds.Contains(f.Id)).ToList();
        context.SetFileSet(filtered);
    }
}