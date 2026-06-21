using ArchiveFlow.Application.Nodes.Definitions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// Represents one visual port on a node instance.
/// Phase 0.3 uses one primary input port and one primary output port per node card.
/// </summary>
public partial class PortInstanceViewModel : ObservableObject
{
    public NodeInstanceViewModel ParentNode { get; }
    public bool IsInput { get; }
    public NodePortDataType DataType { get; }

    public double RelativeX { get; }
    public double RelativeY { get; }

    [ObservableProperty]
    private double _absoluteX;

    [ObservableProperty]
    private double _absoluteY;

    public PortInstanceViewModel(
        NodeInstanceViewModel parentNode,
        bool isInput,
        NodePortDataType dataType,
        double relativeX,
        double relativeY)
    {
        ParentNode = parentNode;
        IsInput = isInput;
        DataType = dataType;
        RelativeX = relativeX;
        RelativeY = relativeY;
    }
}