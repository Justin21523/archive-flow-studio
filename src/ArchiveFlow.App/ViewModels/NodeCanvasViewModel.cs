using ArchiveFlow.Application.Nodes.Definitions;
using ArchiveFlow.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// ViewModel for the node workspace.
/// Phase 0.2 focuses on NodeDefinition-driven library, adding nodes,
/// selecting nodes, and showing a stable Inspector summary.
/// </summary>
public partial class NodeCanvasViewModel : ObservableObject
{
    public ObservableCollection<NodeLibraryCategoryViewModel> NodeLibraryCategories { get; } = new();
    public ObservableCollection<NodeInstanceViewModel> Nodes { get; } = new();
    public ObservableCollection<NodeParameterInstanceViewModel> InspectorParameters { get; } = new();

    [ObservableProperty]
    private NodeInstanceViewModel? _selectedNode;

    [ObservableProperty]
    private string _statusMessage = "Node workspace ready.";

    [ObservableProperty]
    private string _inspectorTitle = "No node selected";

    [ObservableProperty]
    private string _inspectorType = "-";

    [ObservableProperty]
    private string _inspectorCategory = "-";

    [ObservableProperty]
    private string _inspectorSubcategory = "-";

    [ObservableProperty]
    private string _inspectorDescription = "Select a node on the canvas to inspect its definition and parameters.";

    [ObservableProperty]
    private string _inspectorMode = "-";

    [ObservableProperty]
    private string _inspectorPorts = "-";

    [ObservableProperty]
    private string _inspectorParameterSummary = "-";

    public NodeCanvasViewModel(NodeRegistry nodeRegistry)
    {
        BuildNodeLibrary(nodeRegistry);
        AddStarterNodes(nodeRegistry);
    }

    public void AddNodeFromDefinition(NodeDefinition definition)
    {
        var offset = Nodes.Count * 32;
        var node = new NodeInstanceViewModel(definition, 120 + offset, 120 + offset);

        Nodes.Add(node);
        SelectNode(node);

        StatusMessage = $"Added node: {definition.DisplayName}";
    }

    public void SelectNode(NodeInstanceViewModel node)
    {
        if (SelectedNode != null)
        {
            SelectedNode.IsSelected = false;
        }

        SelectedNode = node;
        SelectedNode.IsSelected = true;

        RefreshInspector(node);
    }

    [RelayCommand]
    private void DeleteSelectedNode()
    {
        if (SelectedNode == null)
        {
            return;
        }

        var title = SelectedNode.Title;
        Nodes.Remove(SelectedNode);

        SelectedNode = null;
        ClearInspector();

        StatusMessage = $"Deleted node: {title}";
    }

    [RelayCommand]
    private void ClearCanvas()
    {
        Nodes.Clear();
        SelectedNode = null;
        ClearInspector();

        StatusMessage = "Canvas cleared.";
    }

    private void BuildNodeLibrary(NodeRegistry nodeRegistry)
    {
        NodeLibraryCategories.Clear();

        var categoryGroups = nodeRegistry.GetAll()
            .GroupBy(x => x.Category)
            .OrderBy(x => x.Key.ToString());

        foreach (var categoryGroup in categoryGroups)
        {
            var categoryViewModel = new NodeLibraryCategoryViewModel
            {
                Name = FormatCategoryName(categoryGroup.Key),
                Subcategories = categoryGroup
                    .GroupBy(x => x.Subcategory)
                    .OrderBy(x => x.Key)
                    .Select(subcategory => new NodeLibrarySubcategoryViewModel
                    {
                        Name = subcategory.Key,
                        Nodes = subcategory
                            .OrderBy(x => x.DisplayName)
                            .Select(x => new NodeLibraryItemViewModel(x))
                            .ToList()
                    })
                    .ToList()
            };

            NodeLibraryCategories.Add(categoryViewModel);
        }
    }

    private void AddStarterNodes(NodeRegistry nodeRegistry)
    {
        var allFiles = nodeRegistry.FindByType("source.all_files");
        var resultTable = nodeRegistry.FindByType("output.result_table");

        if (allFiles != null)
        {
            Nodes.Add(new NodeInstanceViewModel(allFiles, 180, 180));
        }

        if (resultTable != null)
        {
            Nodes.Add(new NodeInstanceViewModel(resultTable, 480, 180));
        }

        if (Nodes.Count > 0)
        {
            SelectNode(Nodes[0]);
        }
    }

    private void RefreshInspector(NodeInstanceViewModel node)
    {
        InspectorParameters.Clear();

        foreach (var parameter in node.Parameters)
        {
            InspectorParameters.Add(parameter);
        }

        InspectorTitle = node.Title;
        InspectorType = node.NodeType;
        InspectorCategory = node.Category;
        InspectorSubcategory = node.Subcategory;
        InspectorDescription = node.Description;
        InspectorMode = node.ModeText;
        InspectorPorts = $"Inputs: {node.InputCount}, Outputs: {node.OutputCount}";
        InspectorParameterSummary = node.GetParameterSummary();
    }

    private void ClearInspector()
    {
        InspectorParameters.Clear();

        InspectorTitle = "No node selected";
        InspectorType = "-";
        InspectorCategory = "-";
        InspectorSubcategory = "-";
        InspectorDescription = "Select a node on the canvas to inspect its definition and parameters.";
        InspectorMode = "-";
        InspectorPorts = "-";
        InspectorParameterSummary = "-";
    }

    private static string FormatCategoryName(NodeCategory category)
    {
        return category switch
        {
            NodeCategory.Sources => "Sources",
            NodeCategory.QueryFilters => "Query Filters",
            NodeCategory.Search => "Search",
            NodeCategory.LogicAndSetOperations => "Logic & Set Operations",
            NodeCategory.MetadataActions => "Metadata Actions",
            NodeCategory.FileActions => "File Actions",
            NodeCategory.CreateAndTemplate => "Create / Template",
            NodeCategory.Relationships => "Relationships",
            NodeCategory.IndexingAndExtraction => "Indexing & Extraction",
            NodeCategory.Outputs => "Outputs",
            _ => category.ToString()
        };
    }
}