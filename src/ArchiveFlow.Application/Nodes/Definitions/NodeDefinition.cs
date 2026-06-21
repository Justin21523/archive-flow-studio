namespace ArchiveFlow.Application.Nodes.Definitions;

using ArchiveFlow.Application.Nodes;

/// <summary>
/// Represents the major group shown in the Node Library.
/// </summary>
public enum NodeCategory
{
    Sources,
    QueryFilters,
    Search,
    LogicAndSetOperations,
    MetadataActions,
    FileActions,
    CreateAndTemplate,
    Relationships,
    IndexingAndExtraction,
    Outputs
}

/// <summary>
/// Represents the type of data flowing through a node port.
/// </summary>
public enum NodePortDataType
{
    FileSet,
    SingleFile,
    MetadataSet,
    StringValue,
    NumericValue,
    BooleanValue,
    GraphData,
    Any
}

/// <summary>
/// Represents a parameter control type used by the Inspector.
/// </summary>
public enum NodeParameterControlType
{
    Text,
    Number,
    Boolean,
    Dropdown,
    Date
}

/// <summary>
/// Describes a node input or output port.
/// </summary>
public sealed class NodePortDefinition
{
    public string Name { get; init; } = string.Empty;
    public NodePortDataType DataType { get; init; }
}

/// <summary>
/// Describes a configurable parameter for a node.
/// </summary>
public sealed class NodeParameterDefinition
{
    public string Key { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public NodeParameterControlType ControlType { get; init; } = NodeParameterControlType.Text;
    public string DefaultValue { get; init; } = string.Empty;
    public IReadOnlyList<string> Options { get; init; } = Array.Empty<string>();
    public bool IsRequired { get; init; }
}

/// <summary>
/// Defines a node type. This is the single source of truth for the Node Library,
/// node cards, and the Inspector.
/// </summary>
public sealed class NodeDefinition
{
    public string NodeType { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public NodeCategory Category { get; init; }
    public string Subcategory { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = "◆";
    public string AccentColor { get; init; } = "#607D8B";
    public bool IsPreviewOnly { get; init; } = true;
    public bool IsActionNode { get; init; }

    public IReadOnlyList<NodePortDefinition> InputPorts { get; init; } = Array.Empty<NodePortDefinition>();
    public IReadOnlyList<NodePortDefinition> OutputPorts { get; init; } = Array.Empty<NodePortDefinition>();
    public IReadOnlyList<NodeParameterDefinition> Parameters { get; init; } = Array.Empty<NodeParameterDefinition>();

    public Func<IServiceProvider, IArchiveNode>? Factory { get; init; }
}
