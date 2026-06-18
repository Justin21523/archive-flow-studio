using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// Sorts the current file set by specified field and direction.
/// Parameter format: "field:direction" e.g., "size:desc" or "name:asc"
/// </summary>
public class SortNode : IArchiveNode
{
    public string SortRule { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Sort: {SortRule}";
    public double X { get; set; }
    public double Y { get; set; }

    public SortNode(string sortRule)
    {
        SortRule = sortRule;
    }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(SortRule)) return Task.CompletedTask;

        var parts = SortRule.Split(':', 2);
        if (parts.Length != 2) return Task.CompletedTask;

        var sortField = parts[0].Trim().ToLowerInvariant();
        var direction = parts[1].Trim().ToLowerInvariant();
        bool descending = direction == "desc";

        IOrderedEnumerable<ArchiveFlow.Domain.Entities.FileRecord> sorted;

        switch (sortField)
        {
            case "size":
                sorted = descending 
                    ? context.CurrentFileSet.OrderByDescending(f => f.FileSize)
                    : context.CurrentFileSet.OrderBy(f => f.FileSize);
                break;
            case "name":
            case "filename":
                sorted = descending
                    ? context.CurrentFileSet.OrderByDescending(f => f.FileName)
                    : context.CurrentFileSet.OrderBy(f => f.FileName);
                break;
            case "date":
            case "imported":
                sorted = descending
                    ? context.CurrentFileSet.OrderByDescending(f => f.ImportedAt)
                    : context.CurrentFileSet.OrderBy(f => f.ImportedAt);
                break;
            case "modified":
                sorted = descending
                    ? context.CurrentFileSet.OrderByDescending(f => f.ModifiedAt)
                    : context.CurrentFileSet.OrderBy(f => f.ModifiedAt);
                break;
            case "extension":
            case "type":
                sorted = descending
                    ? context.CurrentFileSet.OrderByDescending(f => f.FileExtension)
                    : context.CurrentFileSet.OrderBy(f => f.FileExtension);
                break;
            default:
                return Task.CompletedTask;
        }

        context.SetFileSet(sorted.ToList());
        return Task.CompletedTask;
    }
}