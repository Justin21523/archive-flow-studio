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

    private void NodeBody_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not NodeInstanceViewModel node)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var canvasView = this.FindAncestorOfType<NodeCanvasView>();
        if (canvasView == null)
        {
            return;
        }

        e.Pointer.Capture(canvasView.WorkspaceCanvas);
        canvasView.StartNodeDrag(node, e.GetPosition(canvasView.WorkspaceCanvas));

        e.Handled = true;
    }

    private void OutputPort_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not NodeInstanceViewModel node || node.OutputPort == null)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var canvasView = this.FindAncestorOfType<NodeCanvasView>();
        if (canvasView == null)
        {
            return;
        }

        e.Pointer.Capture(canvasView.WorkspaceCanvas);
        canvasView.StartConnection(node.OutputPort, e.GetPosition(canvasView.WorkspaceCanvas));

        e.Handled = true;
    }
}