using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes;

/// <summary>
/// Interface for nodes that modify data. They support Preview and Apply mechanisms.
/// </summary>
public interface IActionNode : IArchiveNode
{
    Task<ActionPreview> PreviewAsync(NodeExecutionContext context, CancellationToken cancellationToken = default);
    Task ApplyAsync(NodeExecutionContext context, CancellationToken cancellationToken = default);
}