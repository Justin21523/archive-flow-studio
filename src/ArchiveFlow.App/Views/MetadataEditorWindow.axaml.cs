using Avalonia.Controls;
using Avalonia.Interactivity;
using ArchiveFlow.App.ViewModels;

namespace ArchiveFlow.App.Views;

public partial class MetadataEditorWindow : Window
{
    public MetadataEditorWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}