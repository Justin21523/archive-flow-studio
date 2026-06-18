using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiveFlow.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Set DataContext for Dashboard
        if (this.FindControl<TabItem>("DashboardTab") is TabItem dashboardTab)
        {
            dashboardTab.DataContext = App.Services.GetService<ArchiveFlow.App.ViewModels.DashboardViewModel>();
        }

        // Set DataContext for Database Manager (if exists)
        if (this.FindControl<TabItem>("DbManagerTab") is TabItem dbTab)
        {
            dbTab.DataContext = App.Services.GetService<ArchiveFlow.App.ViewModels.DatabaseManagerViewModel>();
        }

        // Set DataContext for Graph Explorer (if exists)
        if (this.FindControl<TabItem>("GraphExplorerTab") is TabItem graphTab)
        {
            graphTab.DataContext = App.Services.GetService<ArchiveFlow.App.ViewModels.GraphExplorerViewModel>();
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
