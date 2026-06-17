using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ArchiveFlow.App.ViewModels;
using System;

namespace ArchiveFlow.App.Views;

public partial class NodeView : UserControl
{
    private Point _startPoint;
    private bool _isDragging;

    public NodeView()
    {
        InitializeComponent();
    }

    private void Node_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (DataContext is NodeViewModel vm && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _startPoint = e.GetPosition(this.Parent as Control);
            _isDragging = true;
            vm.IsSelected = true;
            
            if (DataContext is NodeViewModel nodeVm)
            {
                // Notify canvas to start drag
                var canvas = this.FindAncestor<NodeCanvasView>();
                canvas?.ViewModel?.StartDrag(nodeVm);
            }
            e.Handled = true;
        }
    }

    private void Node_PointerMoved(object sender, PointerEventArgs e)
    {
        if (_isDragging && DataContext is NodeViewModel vm)
        {
            var currentPoint = e.GetPosition(this.Parent as Control);
            var delta = currentPoint - _startPoint;
            _startPoint = currentPoint;

            var canvas = this.FindAncestor<NodeCanvasView>();
            canvas?.ViewModel?.MoveDrag(delta.X, delta.Y);
        }
    }

    private void Node_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
        var canvas = this.FindAncestor<NodeCanvasView>();
        canvas?.ViewModel?.EndDrag();
    }
}

public static class VisualTreeHelper
{
    public static T? FindAncestor<T>(this Control control) where T : Control
    {
        var parent = control.Parent as Control;
        while (parent != null)
        {
            if (parent is T result) return result;
            parent = parent.Parent as Control;
        }
        return null;
    }
}