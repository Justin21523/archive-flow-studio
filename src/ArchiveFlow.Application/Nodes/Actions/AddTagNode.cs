using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Application.Nodes.Actions;

/// <summary>
/// Action node that adds a specific tag to all files in the current context.
/// Implements IActionNode for safe Preview/Apply workflow.
/// </summary>
public class AddTagNode : IActionNode
{
    private readonly IMetadataRepository _metadataRepo;
    private readonly string _tagName;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Add Tag: {_tagName}";
    public double X { get; set; }
    public double Y { get; set; }

    public AddTagNode(IMetadataRepository metadataRepo, string tagName)
    {
        _metadataRepo = metadataRepo;
        _tagName = tagName;
    }

    public async Task<ActionPreview> PreviewAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        // In a real app, we would check which files already have this tag to give an accurate count.
        // For MVP, we assume all files in context will be affected.
        return await Task.FromResult(new ActionPreview
        {
            NodeName = DisplayName,
            AffectedFileCount = context.CurrentFileSet.Count,
            Description = $"Will add tag '{_tagName}' to {context.CurrentFileSet.Count} files.",
            IsDangerous = false
        });
    }

    public async Task ApplyAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        var field = await _metadataRepo.GetOrCreateFieldAsync("tag", "Tag");
        if (field == null) return;

        foreach (var file in context.CurrentFileSet.ToList())
        {
            await _metadataRepo.AddMetadataValueAsync(file.Id, field.Id, _tagName);
        }
    }

    // Fallback for linear execution if not using Preview/Apply flow
    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        await ApplyAsync(context, cancellationToken);
    }
}