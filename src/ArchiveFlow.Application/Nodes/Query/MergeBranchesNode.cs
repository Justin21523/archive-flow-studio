using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// DAG utility node that combines multiple incoming file streams into a single unique set.
/// Removes duplicates based on file hash.
/// </summary>
public class MergeBranchesNode : IArchiveNode
{
    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => "Merge Branches";
    public double X { get; set; }
    public double Y { get; set; }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        // In a linear topological execution, this node receives the latest context.
        // It deduplicates the current set to ensure clean merging output.
        var uniqueFiles = context.CurrentFileSet
            .GroupBy(f => f.FileHash)
            .Select(g => g.First())
            .ToList();

        context.SetFileSet(uniqueFiles);
        return Task.CompletedTask;
    }
}