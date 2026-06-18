using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveFlow.App.ViewModels;

public partial class EdgeViewModel : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();
    public PortViewModel Source { get; }
    public PortViewModel Target { get; }

    [ObservableProperty]
    private string _pathData = string.Empty;

    public EdgeViewModel(PortViewModel source, PortViewModel target)
    {
        Source = source;
        Target = target;
    }

    public void UpdateGeometry()
    {
        double x1 = Source.AbsoluteX;
        double y1 = Source.AbsoluteY;
        double x2 = Target.AbsoluteX;
        double y2 = Target.AbsoluteY;

        // Calculate control points for a smooth cubic bezier curve
        double dx = Math.Abs(x2 - x1) * 0.5;
        double cx1 = x1 + dx;
        double cy1 = y1;
        double cx2 = x2 - dx;
        double cy2 = y2;

        PathData = $"M {x1},{y1} C {cx1},{cy1} {cx2},{cy2} {x2},{y2}";
    }
}