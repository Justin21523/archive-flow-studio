using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes.Query;

public class PassThroughNode : IArchiveNode
{
    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => "Result";
    public double X { get; set; }
    public double Y { get; set; }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
