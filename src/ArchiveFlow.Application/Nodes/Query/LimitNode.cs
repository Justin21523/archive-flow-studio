using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// Limits the number of files in the current set.
/// Parameter: number of files to keep (e.g., "100")
/// </summary>
public class LimitNode : IArchiveNode
{
    public int MaxCount { get; set; } = 100;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Limit: {MaxCount}";
    public double X { get; set; }
    public double Y { get; set; }

    public LimitNode(int maxCount)
    {
        MaxCount = maxCount;
    }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (MaxCount <= 0) return Task.CompletedTask;

        var limited = context.CurrentFileSet.Take(MaxCount).ToList();
        context.SetFileSet(limited);
        return Task.CompletedTask;
    }
}