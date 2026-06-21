using System.Collections.Generic;
using ArchiveFlow.Application.Nodes;

namespace ArchiveFlow.Application.Interfaces;

/// <summary>
/// Interface that all node plugins must implement.
/// Plugins are loaded dynamically at runtime to extend the system's capabilities.
/// </summary>
public interface INodePlugin
{
    /// <summary>
    /// The unique identifier of the plugin.
    /// </summary>
    string PluginId { get; }

    /// <summary>
    /// The display name of the plugin.
    /// </summary>
    string PluginName { get; }

    /// <summary>
    /// Returns a list of node definitions provided by this plugin.
    /// </summary>
    IEnumerable<INodeDefinition> GetNodeDefinitions();
}

/// <summary>
/// Describes a node type provided by a plugin, including how to instantiate it.
/// </summary>
public class INodeDefinition
{
    public string NodeType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = "Plugins";
    
    /// <summary>
    /// Factory method to create an instance of the backend node.
    /// </summary>
    public Func<IArchiveNode> Factory { get; set; } = null!;
}
