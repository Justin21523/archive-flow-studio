using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// Filters files based on file size range.
/// Parameter format: "min:max" (e.g., "1024:1048576" for 1KB to 1MB). Use -1 for unbounded.
/// </summary>
public class SizeFilterNode : IArchiveNode
{
    public string SizeRule { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Size Filter: {SizeRule}";
    public double X { get; set; }
    public double Y { get; set; }

    public SizeFilterNode(string sizeRule) { SizeRule = sizeRule; }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(SizeRule)) return Task.CompletedTask;

        var parts = SizeRule.Split(':');
        if (parts.Length != 2) return Task.CompletedTask;

        long min = long.TryParse(parts[0], out long v1) ? v1 : 0;
        long max = long.TryParse(parts[1], out long v2) ? v2 : long.MaxValue;

        var filtered = context.CurrentFileSet.Where(f => f.FileSize >= min && f.FileSize <= max).ToList();
        context.SetFileSet(filtered);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Filters files based on a date range (Imported Date).
/// Parameter format: "YYYY-MM-DD:YYYY-MM-DD"
/// </summary>
public class DateRangeFilterNode : IArchiveNode
{
    public string DateRule { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Date Filter: {DateRule}";
    public double X { get; set; }
    public double Y { get; set; }

    public DateRangeFilterNode(string dateRule) { DateRule = dateRule; }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(DateRule)) return Task.CompletedTask;

        var parts = DateRule.Split(':');
        if (parts.Length != 2) return Task.CompletedTask;

        DateTime min = DateTime.TryParse(parts[0], out DateTime d1) ? d1 : DateTime.MinValue;
        DateTime max = DateTime.TryParse(parts[1], out DateTime d2) ? d2 : DateTime.MaxValue;

        var filtered = context.CurrentFileSet.Where(f => f.ImportedAt >= min && f.ImportedAt <= max).ToList();
        context.SetFileSet(filtered);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Filters files where the file path contains a specific string.
/// </summary>
public class PathContainsFilterNode : IArchiveNode
{
    public string SearchString { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Path Contains: {SearchString}";
    public double X { get; set; }
    public double Y { get; set; }

    public PathContainsFilterNode(string searchString) { SearchString = searchString; }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(SearchString)) return Task.CompletedTask;

        var filtered = context.CurrentFileSet
            .Where(f => f.FilePath.Contains(SearchString, StringComparison.OrdinalIgnoreCase))
            .ToList();
        context.SetFileSet(filtered);
        return Task.CompletedTask;
    }
}