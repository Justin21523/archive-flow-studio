using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes;

/// <summary>
/// Interface for nodes that modify data. 
/// They must support a two-step execution: Preview (safe) and Apply (mutation).
/// </summary>
public interface IActionNode : IArchiveNode
{
    /// <summary>
    /// Analyzes the input and returns a description of what WILL be changed, 
    /// without actually modifying any data.
    /// </summary>
    Task<ActionPreview> PreviewAsync(NodeExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actually applies the changes to the data source (Database/File System).
    /// </summary>
    Task ApplyAsync(NodeExecutionContext context, CancellationToken cancellationToken = default);
}

