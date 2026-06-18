using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// Represents a node in the TreeView hierarchy.
/// </summary>
public partial class NodeLibraryItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _nodeType = string.Empty; // Empty for categories

    [ObservableProperty]
    private bool _isCategory;

    [ObservableProperty]
    private ObservableCollection<NodeLibraryItem> _children = new();

    public NodeLibraryItem(string name, string nodeType = "", bool isCategory = false)
    {
        Name = name;
        NodeType = nodeType;
        IsCategory = isCategory;
    }
}