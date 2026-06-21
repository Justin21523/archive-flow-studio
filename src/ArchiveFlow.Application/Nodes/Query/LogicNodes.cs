using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// Sorts the current file set by a specified field and direction.
/// Parameter format: "field:direction" (e.g., "size:desc", "name:asc", "date:desc")
/// </summary>
public class SortNode : IArchiveNode
{
    public string SortRule { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Sort: {SortRule}";
    public double X { get; set; }
    public double Y { get; set; }

    public SortNode(string sortRule) { SortRule = sortRule; }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(SortRule)) return Task.CompletedTask;

        var parts = SortRule.Split(':');
        if (parts.Length != 2) return Task.CompletedTask;

        var field = parts[0].Trim().ToLowerInvariant();
        var direction = parts[1].Trim().ToLowerInvariant();
        bool desc = direction == "desc";

        IOrderedEnumerable<ArchiveFlow.Domain.Entities.FileRecord> sorted;

        switch (field)
        {
            case "size": sorted = desc ? context.CurrentFileSet.OrderByDescending(f => f.FileSize) : context.CurrentFileSet.OrderBy(f => f.FileSize); break;
            case "name": sorted = desc ? context.CurrentFileSet.OrderByDescending(f => f.FileName) : context.CurrentFileSet.OrderBy(f => f.FileName); break;
            case "date": sorted = desc ? context.CurrentFileSet.OrderByDescending(f => f.ImportedAt) : context.CurrentFileSet.OrderBy(f => f.ImportedAt); break;
            default: return Task.CompletedTask;
        }

        context.SetFileSet(sorted.ToList());
        return Task.CompletedTask;
    }
}

/// <summary>
/// Limits the number of files in the current set to the top N.
/// </summary>
public class LimitNode : IArchiveNode
{
    public int MaxCount { get; set; } = 100;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Limit: {MaxCount}";
    public double X { get; set; }
    public double Y { get; set; }

    public LimitNode(int maxCount) { MaxCount = maxCount; }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (MaxCount <= 0) return Task.CompletedTask;
        context.SetFileSet(context.CurrentFileSet.Take(MaxCount).ToList());
        return Task.CompletedTask;
    }
}