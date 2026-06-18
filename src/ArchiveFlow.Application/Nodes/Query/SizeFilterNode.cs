using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// Filters files based on file size.
/// Parameter format: "operator:value" e.g., ">5000000" (5MB)
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

        long sizeBytes;
        string condition = SizeRule.Trim();

        if (condition.EndsWith("MB", StringComparison.OrdinalIgnoreCase))
        {
            if (!long.TryParse(condition.Replace("MB", ""), out long mb)) return Task.CompletedTask;
            sizeBytes = mb * 1024 * 1024;
        }
        else if (condition.EndsWith("KB", StringComparison.OrdinalIgnoreCase))
        {
            if (!long.TryParse(condition.Replace("KB", ""), out long kb)) return Task.CompletedTask;
            sizeBytes = kb * 1024;
        }
        else
        {
            if (!long.TryParse(condition.TrimStart('>', '<', '='), out sizeBytes)) return Task.CompletedTask;
        }

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
}