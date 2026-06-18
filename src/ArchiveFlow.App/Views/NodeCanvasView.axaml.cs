using Avalonia.Controls;
using Avalonia.Input;
using ArchiveFlow.App.ViewModels;
using ArchiveFlow.Application.Nodes.Actions;
using Avalonia;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.App.Views;

public partial class NodeCanvasView : UserControl
{
    private NodeViewModel? _draggedNode;
    private Point _dragOffset;

    public NodeCanvasView()
    {
        InitializeComponent();
    }

    public NodeCanvasViewModel? ViewModel => DataContext as NodeCanvasViewModel;

    private void NodeBody_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (DataContext is NodeViewModel vm && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var canvas = this.FindAncestor<NodeCanvasView>();
            canvas?.StartNodeDrag(vm, canvas.GetCanvasPosition(e), e.Pointer);
            e.Handled = true;
        }
    }

    // 新增：當點擊 TextBox 時，不觸發節點拖曳
    private void TextBox_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        e.Handled = true; 
    }

    private void OutputPort_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (DataContext is NodeViewModel vm && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var canvas = this.FindAncestor<NodeCanvasView>();
            if (canvas != null)
            {
                canvas.StartConnection(vm.OutputPort, canvas.GetCanvasPosition(e));
            }
            e.Handled = true;
        }
    }

    private void InputPort_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (DataContext is NodeViewModel vm)
        {
            var canvas = this.FindAncestor<NodeCanvasView>();
            canvas?.FinishConnection(vm.InputPort);
            e.Handled = true;
        }
    }

    // Called by NodeView when dragging starts
    public Point GetCanvasPosition(PointerEventArgs e)
    {
        return e.GetPosition(MainCanvas);
    }

    public void StartNodeDrag(NodeViewModel node, Point canvasPosition, IPointer pointer)
    {
        _draggedNode = node;
        // Calculate offset so the node doesn't jump to the cursor center
        _dragOffset = new Point(canvasPosition.X - node.X, canvasPosition.Y - node.Y);
        pointer.Capture(MainCanvas);
    }

    private void Canvas_PointerMoved(object sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(MainCanvas);

        if (_draggedNode != null)
        {
            // Update node position directly via ViewModel
            ViewModel?.UpdateNodePosition(_draggedNode, pos.X - _dragOffset.X, pos.Y - _dragOffset.Y);
        }
        else if (ViewModel?.IsConnecting == true)
        {
            // Update temporary connection line
            ViewModel.UpdateTempConnection(pos.X, pos.Y);
        }
    }

    private void Canvas_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (_draggedNode != null)
        {
            _draggedNode = null;
            e.Pointer.Capture(null);
        }
        else if (ViewModel?.IsConnecting == true)
        {
            var pos = e.GetPosition(MainCanvas);
            if (!ViewModel.TryFinishConnectionAt(pos.X, pos.Y))
            {
                ViewModel.CancelConnection();
            }
        }
    }

    // Called by NodeView when starting connection from Output Port
    public void StartConnection(PortViewModel port, Point canvasPosition)
    {
        ViewModel?.StartConnection(port);
        ViewModel?.UpdateTempConnection(canvasPosition.X, canvasPosition.Y);
    }

    // Called by NodeView when releasing on Input Port
    public void FinishConnection(PortViewModel port)
    {
        ViewModel?.FinishConnection(port);
    }

    private void ResultList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is FileRecord file)
        {
            ViewModel?.SelectFile(file);
        }
    }
}
