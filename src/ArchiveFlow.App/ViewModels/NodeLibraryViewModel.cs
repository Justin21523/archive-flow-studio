using System.Collections.Generic;
using ArchiveFlow.Application.Nodes.Definitions;
using Avalonia.Media;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// Represents a category group in the Node Library.
/// </summary>
public sealed class NodeLibraryCategoryViewModel
{
    public string Name { get; init; } = string.Empty;
    public IList<NodeLibrarySubcategoryViewModel> Subcategories { get; init; } = new List<NodeLibrarySubcategoryViewModel>();
}

/// <summary>
/// Represents a subcategory group in the Node Library.
/// </summary>
public sealed class NodeLibrarySubcategoryViewModel
{
    public string Name { get; init; } = string.Empty;
    public IList<NodeLibraryItemViewModel> Nodes { get; init; } = new List<NodeLibraryItemViewModel>();
}

/// <summary>
/// Represents one clickable node definition in the Node Library.
/// </summary>
public sealed class NodeLibraryItemViewModel
{
    public NodeDefinition Definition { get; }

    public string DisplayName => Definition.DisplayName;
    public string Description => Definition.Description;
    public string Icon => Definition.Icon;
    public string BadgeText => Definition.IsActionNode ? "ACTION" : "PREVIEW";
    public IBrush AccentBrush { get; }
    public IBrush BadgeBrush { get; }

    public NodeLibraryItemViewModel(NodeDefinition definition)
    {
        Definition = definition;
        AccentBrush = Brush.Parse(definition.AccentColor);
        BadgeBrush = definition.IsActionNode
            ? Brush.Parse("#FF9800")
            : Brush.Parse("#4CAF50");
    }
}