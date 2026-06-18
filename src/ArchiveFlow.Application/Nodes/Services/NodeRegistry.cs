using System.Collections.Generic;
using System.Linq;
using ArchiveFlow.Application.Nodes.Definitions;

namespace ArchiveFlow.Application.Services;

/// <summary>
/// Global registry for all available node definitions.
/// </summary>
public class NodeRegistry
{
    private readonly Dictionary<string, NodeDefinition> _definitions = new();

    public void Register(NodeDefinition definition)
    {
        _definitions[definition.NodeType] = definition;
    }

    public NodeDefinition? GetDefinition(string nodeType)
    {
        return _definitions.TryGetValue(nodeType, out var def) ? def : null;
    }

    public IEnumerable<NodeDefinition> GetAllDefinitions()
    {
        return _definitions.Values;
    }

    public IEnumerable<NodeDefinition> GetByCategory(NodeCategory category)
    {
        return _definitions.Values.Where(d => d.Category == category);
    }

    public IEnumerable<NodeDefinition> GetDefinitionsByCategory(string category)
    {
        return _definitions.Values.Where(d => d.Category.ToString().Equals(category, System.StringComparison.OrdinalIgnoreCase));
    }
}
