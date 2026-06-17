using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveFlow.App.ViewModels;

public partial class NodeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private string _status = "Idle"; // Idle, Running, Success, Error

    public string NodeId { get; }

    public NodeViewModel(string nodeId, string title, double x, double y)
    {
        NodeId = nodeId;
        Title = title;
        X = x;
        Y = y;
    }
}