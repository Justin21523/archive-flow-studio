using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Application.Workflows;

public class WorkflowEngine
{
    private readonly ILogger<WorkflowEngine> _logger;

    public WorkflowEngine(ILogger<WorkflowEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes a list of nodes in a linear sequence.
    /// </summary>
    public async Task<Nodes.NodeExecutionContext> ExecuteLinearAsync(
        IEnumerable<Nodes.IArchiveNode> nodes, 
        CancellationToken cancellationToken = default)
    {
        var context = new Nodes.NodeExecutionContext();
        
        foreach (var node in nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation("Executing node: {NodeName} ({NodeId})", node.DisplayName, node.Id);
            
            await node.ExecuteAsync(context, cancellationToken);
            
            _logger.LogInformation("Node {NodeName} completed. Current file set count: {Count}", 
                node.DisplayName, context.CurrentFileSet.Count);
        }

        return context;
    }
}