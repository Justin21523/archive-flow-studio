using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ArchiveFlow.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Exit_Click(object? sender, RoutedEventArgs e) => Close();

    private void GoToDashboard_Click(object? sender, RoutedEventArgs e)
    {
        if (MainTabControl != null) MainTabControl.SelectedIndex = 0;
    }

    private void GoToWorkspace_Click(object? sender, RoutedEventArgs e)
    {
        if (MainTabControl != null) MainTabControl.SelectedIndex = 1;
    }
}