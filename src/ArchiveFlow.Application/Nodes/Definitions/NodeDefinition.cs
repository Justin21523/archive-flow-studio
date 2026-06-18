using System;
using System.Collections.Generic;

namespace ArchiveFlow.Application.Nodes.Definitions;

/// <summary>
/// Defines the high-level categories for nodes in the library.
/// </summary>
public enum NodeCategory 
{ 
    Source, 
    Processor, 
    Action, 
    Output, 
    Relationship 
}

/// <summary>
/// Defines the data type that can flow through node ports.
/// </summary>
public enum PortDataType 
{ 
    FileSet, 
    SingleFile, 
    StringValue, 
    NumericValue, 
    BooleanValue, 
    Any 
}

/// <summary>
/// Represents an input or output port on a node.
/// </summary>
public class PortDefinition
{
    public string Name { get; set; } = string.Empty;
    public PortDataType DataType { get; set; }
    public bool IsInput { get; set; }
}

/// <summary>
/// Represents a configurable parameter for a node (e.g., Text, Dropdown).
/// </summary>
public class ParameterDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "Text"; // Text, Number, Dropdown, Toggle
    public string DefaultValue { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new(); // Used for Dropdown
}

/// <summary>
/// The core metadata definition for a Node. 
/// Acts as the single source of truth for UI rendering and Engine instantiation.
/// </summary>
public class NodeDefinition
{
    public string NodeType { get; set; } = string.Empty; // Unique ID (e.g., "AllFiles", "FilterTxt")
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public NodeCategory Category { get; set; }
    public string SubCategory { get; set; } = string.Empty; // e.g., "Filters", "Metadata"
    
    /// <summary>
    /// True for Query/Preview nodes, False for Action/Mutation nodes.
    /// </summary>
    public bool IsPreviewOnly { get; set; } 
    
    public string AccentColor { get; set; } = "#007ACC"; // Theme color for the node card
    
    public List<PortDefinition> Ports { get; set; } = new();
    public List<ParameterDefinition> Parameters { get; set; } = new();
    
    /// <summary>
    /// Factory method to instantiate the backend node using DI.
    /// </summary>
    public Func<IServiceProvider, IArchiveNode> Factory { get; set; } = null!;
    
    /// <summary>
    /// Optional action to inject UI parameters into the backend node after instantiation.
    /// Keeps Clean Architecture intact (Application layer doesn't need to know about UI ViewModels).
    /// </summary>
    public Action<IArchiveNode, Dictionary<string, string>>? ApplyParameters { get; set; }
}