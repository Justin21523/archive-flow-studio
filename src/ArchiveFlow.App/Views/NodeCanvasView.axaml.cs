using Avalonia.Controls;
using Avalonia.Input;
using ArchiveFlow.App.ViewModels;
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

    // Called by NodeView when dragging starts
    public void StartNodeDrag(NodeViewModel node, Point canvasPosition)
    {
        _draggedNode = node;
        // Calculate offset so the node doesn't jump to the cursor center
        _dragOffset = new Point(canvasPosition.X - node.X, canvasPosition.Y - node.Y);
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
        }
        else if (ViewModel?.IsConnecting == true)
        {
            // Released on empty canvas space -> cancel connection
            ViewModel.CancelConnection();
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