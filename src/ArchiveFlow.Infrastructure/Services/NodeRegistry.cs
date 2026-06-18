using System;
using System.Collections.Generic;
using System.Linq;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Application.Services;

/// <summary>
/// Global registry for all available node types, including built-in and plugin-provided nodes.
/// </summary>
public class NodeRegistry
{
    private readonly Dictionary<string, NodeDefinition> _definitions = new();

    public void Register(NodeDefinition definition)
    {
        if (!_definitions.ContainsKey(definition.NodeType))
        {
            _definitions[definition.NodeType] = definition;
        }
    }

    public NodeDefinition? GetDefinition(string nodeType)
    {
        return _definitions.TryGetValue(nodeType, out var def) ? def : null;
    }

    public IEnumerable<NodeDefinition> GetAllDefinitions()
    {
        return _definitions.Values;
    }

    public IEnumerable<NodeDefinition> GetDefinitionsByCategory(string category)
    {
        return _definitions.Values.Where(d => d.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
    }
}