using ArchiveFlow.App.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace ArchiveFlow.App.Views;

public partial class NodeView : UserControl
{
    public NodeView()
    {
        InitializeComponent();
    }

    private void Node_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not NodeInstanceViewModel node)
        {
            return;
        }

        var canvasView = this.FindAncestorOfType<NodeCanvasView>();
        canvasView?.SelectNodeFromCard(node);

        e.Handled = true;
    }
}