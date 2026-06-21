using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Infrastructure.Services;

/// <summary>
/// Global registry for all available node types, including built-in and plugin-provided nodes.
/// </summary>
public class LegacyPluginNodeRegistry
{
    private readonly Dictionary<string, INodeDefinition> _definitions = new();

    public void Register(INodeDefinition definition)
    {
        if (!_definitions.ContainsKey(definition.NodeType))
        {
            _definitions[definition.NodeType] = definition;
        }
    }

    public INodeDefinition? GetDefinition(string nodeType)
    {
        return _definitions.TryGetValue(nodeType, out var def) ? def : null;
    }

    public IEnumerable<INodeDefinition> GetAllDefinitions()
    {
        return _definitions.Values;
    }

    public IEnumerable<INodeDefinition> GetDefinitionsByCategory(string category)
    {
        return _definitions.Values.Where(d => d.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
    }
}
