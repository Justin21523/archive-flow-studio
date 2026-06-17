using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Domain.Enums;

namespace ArchiveFlow.Application.Nodes.Actions;

public class UpdateStatusNode : IActionNode
{
    private readonly FileStatus _newStatus;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Set Status: {_newStatus}";
    public double X { get; set; }
    public double Y { get; set; }

    public UpdateStatusNode(FileStatus newStatus)
    {
        _newStatus = newStatus;
    }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        // For linear execution fallback, we just apply it.
        return ApplyAsync(context, cancellationToken);
    }

    public Task<ActionPreview> PreviewAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ActionPreview
        {
            AffectedFileCount = context.CurrentFileSet.Count,
            Description = $"Will update status of {context.CurrentFileSet.Count} files to '{_newStatus}'."
        });
    }

    public Task ApplyAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        foreach (var file in context.CurrentFileSet.ToList())
        {
            file.UpdateStatus(_newStatus);
        }
        return Task.CompletedTask;
    }
}