using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes.Query;

public class FileTypeFilterNode : IArchiveNode
{
    private readonly string _targetExtension;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Filter: {_targetExtension}";
    public double X { get; set; }
    public double Y { get; set; }

    public FileTypeFilterNode(string targetExtension)
    {
        _targetExtension = targetExtension.ToLowerInvariant().TrimStart('.');
    }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        var filteredFiles = context.CurrentFileSet
            .Where(f => f.FileExtension.TrimStart('.').Equals(_targetExtension, StringComparison.OrdinalIgnoreCase))
            .ToList();

        context.SetFileSet(filteredFiles);
        return Task.CompletedTask;
    }
}