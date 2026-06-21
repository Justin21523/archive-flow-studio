using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// Limits the current file set to a maximum number of files.
/// </summary>
public class LimitNode : IArchiveNode
{
    public int MaxResults { get; set; }
    public int MaxCount
    {
        get => MaxResults;
        set => MaxResults = value;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Limit: {MaxResults}";
    public double X { get; set; }
    public double Y { get; set; }

    public LimitNode(int maxResults = 100)
    {
        MaxResults = maxResults;
    }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (MaxResults < 0)
        {
            MaxResults = 0;
        }

        context.SetFileSet(context.CurrentFileSet.Take(MaxResults).ToList());
        return Task.CompletedTask;
    }
}
