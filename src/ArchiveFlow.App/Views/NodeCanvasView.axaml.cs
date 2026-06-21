using ArchiveFlow.Application.Nodes.Definitions;
using ArchiveFlow.App.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ArchiveFlow.App.Views;

public partial class NodeCanvasView : UserControl
{
    public NodeCanvasView()
    {
        InitializeComponent();
    }

    public void SelectNodeFromCard(NodeInstanceViewModel node)
    {
        if (DataContext is NodeCanvasViewModel viewModel)
        {
            viewModel.SelectNode(node);
        }
    }

    private void AddNodeButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (button.Tag is not NodeDefinition definition)
        {
            return;
        }

        if (DataContext is NodeCanvasViewModel viewModel)
        {
            viewModel.AddNodeFromDefinition(definition);
        }
    }
}