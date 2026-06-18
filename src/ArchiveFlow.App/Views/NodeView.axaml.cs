using System;
using Avalonia.Controls;
using Avalonia.Input;
using ArchiveFlow.App.ViewModels;

namespace ArchiveFlow.App.Views;

public partial class NodeView : UserControl
{
    public NodeView() { InitializeComponent(); }

    private void NodeBody_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.Source is Control sourceControl &&
            (sourceControl is TextBox || sourceControl.FindAncestor<TextBox>() != null))
        {
            return;
        }

        if (DataContext is NodeViewModel vm && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var canvas = this.FindAncestor<NodeCanvasView>();
            if (canvas != null)
            {
                canvas.StartNodeDrag(vm, canvas.GetCanvasPosition(e), e.Pointer);
            }
            e.Handled = true;
        }
    }

    private void TextBox_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        e.Handled = false; // 允許 TextBox 接收輸入
    }

    private void OutputPort_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (DataContext is NodeViewModel vm && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var canvas = this.FindAncestor<NodeCanvasView>();
            if (canvas != null)
            {
                canvas.StartConnection(vm.OutputPort, canvas.GetCanvasPosition(e), e.Pointer);
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
}

public static class VisualTreeExtensions
{
    public static T? FindAncestor<T>(this Control control) where T : Control
    {
        var parent = control.Parent as Control;
        while (parent != null) { if (parent is T result) return result; parent = parent.Parent as Control; }
        return null;
    }
}
