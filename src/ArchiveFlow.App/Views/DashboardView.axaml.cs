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
        
        // Attach click event to the Generate Data card
        if (this.FindControl<Border>("GenerateDataCard") is Border generateCard)
        {
            generateCard.PointerPressed += async (s, e) =>
            {
                // Resolve service from DI
                var mockService = App.Services.GetService<IMockDataService>();
                if (mockService != null)
                {
                    await mockService.GenerateMockDataAsync();
                    // Optional: Show a message or refresh UI
                }
            };
        }
    }
}
