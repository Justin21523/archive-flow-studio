using Avalonia.Controls;
using Avalonia.Interactivity;
using ArchiveFlow.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiveFlow.App.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        
        // Attach click events to cards
        if (this.FindControl<Border>("WorkspaceCard") is Border workspaceCard)
        {
            workspaceCard.PointerPressed += (s, e) => 
            {
                // Navigate to Workspace tab (handled by MainWindow logic usually, but here we can trigger it)
                // For simplicity, we rely on the TabControl in MainWindow
            };
        }

        if (this.FindControl<Border>("GenerateDataCard") is Border generateCard)
        {
            generateCard.PointerPressed += async (s, e) =>
            {
                var mockService = App.Services.GetService<IMockDataService>();
                if (mockService != null)
                {
                    await mockService.GenerateMockDataAsync();
                    // Refresh statistics after generation
                    if (DataContext is ViewModels.DashboardViewModel vm)
                    {
                        await vm.LoadStatisticsCommand.ExecuteAsync(null);
                    }
                }
            };
        }
    }
}
