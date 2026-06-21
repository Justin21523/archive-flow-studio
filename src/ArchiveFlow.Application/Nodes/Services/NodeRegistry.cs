using ArchiveFlow.Application.Nodes.Definitions;

namespace ArchiveFlow.Application.Services;

/// <summary>
/// Stores all available node definitions.
/// </summary>
public sealed class NodeRegistry
{
    private readonly List<NodeDefinition> _definitions;

    public NodeRegistry()
    {
        _definitions = BuiltInNodeDefinitions.CreateAll()
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Subcategory)
            .ThenBy(x => x.DisplayName)
            .ToList();
    }

    public IReadOnlyList<NodeDefinition> GetAll()
    {
        return _definitions
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Subcategory)
            .ThenBy(x => x.DisplayName)
            .ToList();
    }

    public NodeDefinition? FindByType(string nodeType)
    {
        return _definitions.FirstOrDefault(x => x.NodeType == nodeType);
    }

    public void Register(NodeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var existingIndex = _definitions.FindIndex(x => x.NodeType == definition.NodeType);
        if (existingIndex >= 0)
        {
            _definitions[existingIndex] = definition;
            return;
        }

        _definitions.Add(definition);
    }
}
