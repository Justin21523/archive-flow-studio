using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiveFlow.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Find the Database Manager TabItem and set its DataContext
        if (this.FindControl<TabItem>("DbManagerTab") is TabItem dbTab)
        {
            dbTab.DataContext = App.Services.GetService<ArchiveFlow.App.ViewModels.DatabaseManagerViewModel>();
        }
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
