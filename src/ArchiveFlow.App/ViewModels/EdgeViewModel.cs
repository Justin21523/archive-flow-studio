using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// Represents a Bezier connection between an output port and an input port.
/// Geometry is recalculated centrally by NodeCanvasViewModel.
/// </summary>
public partial class EdgeViewModel : ObservableObject
{
    public string Id { get; } = Guid.NewGuid().ToString("N");

    public PortInstanceViewModel SourcePort { get; }
    public PortInstanceViewModel TargetPort { get; }

    public NodeInstanceViewModel SourceNode => SourcePort.ParentNode;
    public NodeInstanceViewModel TargetNode => TargetPort.ParentNode;

    [ObservableProperty]
    private string _pathData = "M 0,0 C 0,0 0,0 0,0";

    [ObservableProperty]
    private bool _isSelected;

    public IBrush StrokeBrush => IsSelected ? Brushes.Orange : Brushes.DeepSkyBlue;
    public double StrokeThickness => IsSelected ? 4 : 3;

    public EdgeViewModel(PortInstanceViewModel sourcePort, PortInstanceViewModel targetPort)
    {
        SourcePort = sourcePort;
        TargetPort = targetPort;
        UpdatePath();
    }

    public void UpdatePath()
    {
        var x1 = SourcePort.AbsoluteX;
        var y1 = SourcePort.AbsoluteY;
        var x2 = TargetPort.AbsoluteX;
        var y2 = TargetPort.AbsoluteY;

        var distance = Math.Abs(x2 - x1);
        var controlOffset = Math.Max(80, distance * 0.5);

        var cx1 = x1 + controlOffset;
        var cy1 = y1;
        var cx2 = x2 - controlOffset;
        var cy2 = y2;

        PathData = $"M {x1},{y1} C {cx1},{cy1} {cx2},{cy2} {x2},{y2}";
    }

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(StrokeBrush));
        OnPropertyChanged(nameof(StrokeThickness));
    }
}