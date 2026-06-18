using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveFlow.App.ViewModels;

public partial class NodeViewModel : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _nodeType = string.Empty; // e.g., "AllFiles", "Filter", "Result"

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private string _status = "Idle";

    public PortViewModel InputPort { get; }
    public PortViewModel OutputPort { get; }

    public NodeViewModel(string title, string nodeType, double x, double y)
    {
        Title = title;
        NodeType = nodeType;
        X = x;
        Y = y;
        
        // Ports are relative to the node's top-left corner. 
        // Node size is assumed to be 200x100 in UI.
        InputPort = new PortViewModel(this, true, 0, 50);
        OutputPort = new PortViewModel(this, false, 200, 50);
    }
}

public partial class PortViewModel : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();
    public NodeViewModel ParentNode { get; }
    public bool IsInput { get; }
    
    // Relative position within the NodeView
    public double RelativeX { get; }
    public double RelativeY { get; }

    // Absolute position on the Canvas (calculated by Canvas)
    [ObservableProperty]
    private double _absoluteX;

    [ObservableProperty]
    private double _absoluteY;

    public PortViewModel(NodeViewModel parentNode, bool isInput, double relX, double relY)
    {
        ParentNode = parentNode;
        IsInput = isInput;
        RelativeX = relX;
        RelativeY = relY;
    }
}