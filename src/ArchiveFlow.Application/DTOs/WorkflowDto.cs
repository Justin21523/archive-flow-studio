namespace ArchiveFlow.Application.DTOs;

public class WorkflowDto
{
    public string Name { get; set; } = "Untitled Workflow";
    public List<NodeDto> Nodes { get; set; } = new();
    public List<ConnectionDto> Connections { get; set; } = new();
}

public class NodeDto
{
    public string Id { get; set; } = string.Empty; // 使用 String 儲存 Guid
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public string Parameter { get; set; } = string.Empty;
}

public class ConnectionDto
{
    public string SourceNodeId { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
}