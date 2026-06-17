using Avalonia.Controls;
using ArchiveFlow.App.ViewModels;

namespace ArchiveFlow.App.Views;

public partial class NodeCanvasView : UserControl
{
    public NodeCanvasView()
    {
        InitializeComponent();
    }

    public NodeCanvasViewModel? ViewModel => DataContext as NodeCanvasViewModel;
}