using Avalonia.Controls;
using Avalonia.Input;
using Avalonia;
using ArchiveFlow.Browser.ViewModels;

namespace ArchiveFlow.Browser.Views;

public partial class MainView : UserControl
{
    private BrowserWorkflowNode? _draggedNode;
    private Point _dragOffset;
    private bool _isPanning;
    private Point _lastPanPosition;

    public MainView()
    {
        InitializeComponent();
    }

    private BrowserDemoViewModel? ViewModel => DataContext as BrowserDemoViewModel;

    private Control WorkspaceSurfaceControl => this.FindControl<Control>("WorkspaceSurface")!;

    private void WorkflowNode_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control || control.DataContext is not BrowserWorkflowNode node)
        {
            return;
        }

        var point = e.GetCurrentPoint(WorkspaceSurfaceControl);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        ViewModel?.SelectWorkflowNode(node);
        _draggedNode = node;
        var canvasPosition = e.GetPosition(WorkspaceSurfaceControl);
        _dragOffset = new Point(canvasPosition.X - node.X, canvasPosition.Y - node.Y);
        e.Pointer.Capture(WorkspaceSurfaceControl);
        e.Handled = true;
    }

    private void WorkspaceCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(WorkspaceSurfaceControl);
        if (point.Properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _lastPanPosition = e.GetPosition(this);
            e.Pointer.Capture(WorkspaceSurfaceControl);
            e.Handled = true;
        }
    }

    private void WorkspaceCanvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPanning)
        {
            var position = e.GetPosition(this);
            var delta = position - _lastPanPosition;
            _lastPanPosition = position;
            ViewModel?.PanCanvas(delta.X, delta.Y);
            e.Handled = true;
            return;
        }

        if (_draggedNode is not null)
        {
            var position = e.GetPosition(WorkspaceSurfaceControl);
            ViewModel?.MoveWorkflowNode(
                _draggedNode,
                position.X - _dragOffset.X,
                position.Y - _dragOffset.Y);
            e.Handled = true;
        }
    }

    private void WorkspaceCanvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning || _draggedNode is not null)
        {
            _isPanning = false;
            _draggedNode = null;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private void WorkspaceCanvas_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        ViewModel?.ZoomCanvas(e.Delta.Y > 0 ? 1.1 : 0.9);
        e.Handled = true;
    }
}
