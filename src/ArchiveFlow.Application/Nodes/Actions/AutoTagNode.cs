using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Application.Nodes.Actions;

/// <summary>
/// Action node that triggers the auto-tagging service for all files in the current context.
/// </summary>
public class AutoTagNode : IArchiveNode
{
    private readonly IAutoTaggingService _taggingService;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => "Auto-Tag Files";
    public double X { get; set; }
    public double Y { get; set; }

    public AutoTagNode(IAutoTaggingService taggingService)
    {
        _taggingService = taggingService;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        foreach (var file in context.CurrentFileSet.ToList())
        {
            await _taggingService.ApplyAutoTagsAsync(file);
        }
        
        // Auto-tagging is a side-effect; it does not modify the file set flow
    }
}