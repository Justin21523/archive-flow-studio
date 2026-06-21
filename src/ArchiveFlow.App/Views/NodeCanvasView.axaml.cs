using Avalonia.Controls.Shapes;
using ArchiveFlow.Application.Nodes.Definitions;
using ArchiveFlow.App.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ArchiveFlow.App.Views;

public partial class NodeCanvasView : UserControl
{
    private NodeInstanceViewModel? _draggedNode;
    private Point _dragOffset;

    public NodeCanvasView()
    {
        InitializeComponent();
    }

    public Canvas WorkspaceCanvasControl => this.FindControl<Canvas>("WorkspaceCanvas")!;

    private NodeCanvasViewModel? ViewModel => DataContext as NodeCanvasViewModel;

    public void StartNodeDrag(NodeInstanceViewModel node, Point canvasPosition)
    {
        ViewModel?.SelectNode(node);

        _draggedNode = node;
        _dragOffset = new Point(
            canvasPosition.X - node.X,
            canvasPosition.Y - node.Y);
    }

    public void StartConnection(PortInstanceViewModel sourcePort, Point canvasPosition)
    {
        ViewModel?.StartConnection(sourcePort);
        ViewModel?.UpdateTempConnection(canvasPosition.X, canvasPosition.Y);
    }

    private void WorkspaceCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ReferenceEquals(e.Source, WorkspaceCanvas))
        {
            ViewModel?.ClearSelection();
        }
    }

    private void WorkspaceCanvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        var position = e.GetPosition(WorkspaceCanvas);

        if (_draggedNode != null)
        {
            ViewModel?.UpdateNodePosition(
                _draggedNode,
                position.X - _dragOffset.X,
                position.Y - _dragOffset.Y);

            e.Handled = true;
            return;
        }

        if (ViewModel?.IsConnecting == true)
        {
            ViewModel.UpdateTempConnection(position.X, position.Y);
            e.Handled = true;
        }
    }

    private void WorkspaceCanvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var position = e.GetPosition(WorkspaceCanvas);

        if (_draggedNode != null)
        {
            _draggedNode = null;
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }

        if (ViewModel?.IsConnecting == true)
        {
            ViewModel.TryFinishConnectionAt(position.X, position.Y);
            e.Pointer.Capture(null);
            e.Handled = true;
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

        ViewModel?.AddNodeFromDefinition(definition);
    }

    private void Edge_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Path path && path.DataContext is EdgeViewModel edge)
        {
            ViewModel?.SelectEdge(edge);
            e.Handled = true;
        }
    }
}