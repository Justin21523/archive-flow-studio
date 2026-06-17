using System;
using System.Collections.ObjectModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveFlow.App.ViewModels;

public partial class NodeViewModel : ObservableObject
{
    public Guid NodeId { get; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private bool _isSelected;

    public ObservableCollection<EdgeViewModel> OutputEdges { get; } = new();

    public NodeViewModel(string title, double x, double y)
    {
        NodeId = Guid.NewGuid();
        Title = title;
        X = x;
        Y = y;
    }
}

public partial class EdgeViewModel : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();
    public NodeViewModel Source { get; }
    public NodeViewModel Target { get; }

    [ObservableProperty]
    private Point _startPoint;

    [ObservableProperty]
    private Point _endPoint;

    public EdgeViewModel(NodeViewModel source, NodeViewModel target)
    {
        Source = source;
        Target = target;
        
        // Initial calculation
        UpdatePoints();

        // Listen to position changes of source and target nodes
        Source.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == nameof(NodeViewModel.X) || e.PropertyName == nameof(NodeViewModel.Y))
                UpdatePoints();
        };
        
        Target.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == nameof(NodeViewModel.X) || e.PropertyName == nameof(NodeViewModel.Y))
                UpdatePoints();
        };
    }

    private void UpdatePoints()
    {
        // Assuming NodeView size is roughly 200x100
        // Connect from right-center of Source to left-center of Target
        StartPoint = new Point(Source.X + 200, Source.Y + 50);
        EndPoint = new Point(Target.X, Target.Y + 50);
    }
}