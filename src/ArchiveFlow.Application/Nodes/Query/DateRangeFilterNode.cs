using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// Filters files based on date range (imported_at or modified_at).
/// Parameter format: "YYYY-MM-DD:YYYY-MM-DD" or "field:operator:value" e.g., "imported:>2024-01-01".
/// </summary>
public class DateRangeFilterNode : IArchiveNode
{
    public string DateRule { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Date Filter: {DateRule}";
    public double X { get; set; }
    public double Y { get; set; }

    public DateRangeFilterNode(string dateRule)
    {
        DateRule = dateRule;
    }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(DateRule)) return Task.CompletedTask;

        var parts = DateRule.Split(':');
        if (parts.Length == 2 && DateTime.TryParse(parts[0], out var minDate) && DateTime.TryParse(parts[1], out var maxDate))
        {
            var rangeFiltered = context.CurrentFileSet
                .Where(file => file.ImportedAt >= minDate && file.ImportedAt <= maxDate)
                .ToList();

            context.SetFileSet(rangeFiltered);
            return Task.CompletedTask;
        }

        if (parts.Length < 2) return Task.CompletedTask;

        var dateField = parts[0].Trim().ToLowerInvariant();
        var condition = string.Join(':', parts.Skip(1)).Trim();

        if (!DateTime.TryParse(condition.TrimStart('>', '<', '='), out var targetDate))
        {
            return Task.CompletedTask;
        }

        var filtered = context.CurrentFileSet.Where(file =>
        {
            DateTime fileDate = dateField == "modified" 
                ? (file.ModifiedAt ?? file.CreatedAt) 
                : file.ImportedAt;

            if (condition.StartsWith(">=")) return fileDate >= targetDate;
            if (condition.StartsWith("<=")) return fileDate <= targetDate;
            if (condition.StartsWith(">")) return fileDate > targetDate;
            if (condition.StartsWith("<")) return fileDate < targetDate;
            return fileDate.Date == targetDate.Date;
        }).ToList();

        context.SetFileSet(filtered);
        return Task.CompletedTask;
    }
}
