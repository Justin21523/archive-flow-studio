using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// Filters files based on file size.
/// Parameter format: "min:max" or an operator expression such as ">5000000" or "<=10MB".
/// </summary>
public class SizeFilterNode : IArchiveNode
{
    public string SizeRule { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Size Filter: {SizeRule}";
    public double X { get; set; }
    public double Y { get; set; }

    public SizeFilterNode(string sizeRule)
    {
        SizeRule = sizeRule;
    }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(SizeRule)) return Task.CompletedTask;

        string condition = SizeRule.Trim();

        var rangeParts = condition.Split(':', 2);
        if (rangeParts.Length == 2)
        {
            var min = ParseSize(rangeParts[0], 0);
            var max = ParseSize(rangeParts[1], long.MaxValue);

            var rangeFiltered = context.CurrentFileSet
                .Where(file => file.FileSize >= min && file.FileSize <= max)
                .ToList();

            context.SetFileSet(rangeFiltered);
            return Task.CompletedTask;
        }

        var sizeBytes = ParseSize(condition.TrimStart('>', '<', '='), -1);
        if (sizeBytes < 0) return Task.CompletedTask;

        var filtered = context.CurrentFileSet.Where(file =>
        {
            if (condition.StartsWith(">=")) return file.FileSize >= sizeBytes;
            if (condition.StartsWith("<=")) return file.FileSize <= sizeBytes;
            if (condition.StartsWith(">")) return file.FileSize > sizeBytes;
            if (condition.StartsWith("<")) return file.FileSize < sizeBytes;
            return file.FileSize == sizeBytes;
        }).ToList();

        context.SetFileSet(filtered);
        return Task.CompletedTask;
    }

    private static long ParseSize(string value, long fallback)
    {
        var condition = value.Trim();

        if (condition.EndsWith("MB", StringComparison.OrdinalIgnoreCase))
        {
            return long.TryParse(condition[..^2].Trim(), out var mb) ? mb * 1024 * 1024 : fallback;
        }

        if (condition.EndsWith("KB", StringComparison.OrdinalIgnoreCase))
        {
            return long.TryParse(condition[..^2].Trim(), out var kb) ? kb * 1024 : fallback;
        }

        return long.TryParse(condition, out var bytes) ? bytes : fallback;
    }
}
