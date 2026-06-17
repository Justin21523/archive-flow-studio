using System;

namespace ArchiveFlow.Application.Nodes;

/// <summary>
/// Represents a directed edge between two nodes in the workflow DAG.
/// </summary>
public class NodeConnection
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid SourceNodeId { get; set; }
    public Guid TargetNodeId { get; set; }

    public NodeConnection(Guid sourceNodeId, Guid targetNodeId)
    {
        SourceNodeId = sourceNodeId;
        TargetNodeId = targetNodeId;
    }
}