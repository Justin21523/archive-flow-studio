using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes.Query;

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
