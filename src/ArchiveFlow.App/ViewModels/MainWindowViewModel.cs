using ArchiveFlow.Infrastructure.Database;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// Root ViewModel for the main application window.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    public NodeCanvasViewModel NodeCanvas { get; }

    [ObservableProperty]
    private string _title = "ArchiveFlow Studio";

    [ObservableProperty]
    private string _statusMessage = "Application initialized.";

    [ObservableProperty]
    private string _databasePath = string.Empty;

    public MainWindowViewModel(
        IDatabaseConnectionFactory databaseConnectionFactory,
        NodeCanvasViewModel nodeCanvas)
    {
        DatabasePath = databaseConnectionFactory.DatabasePath;
        NodeCanvas = nodeCanvas;
    }
}