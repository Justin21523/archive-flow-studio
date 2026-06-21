using System;
using ArchiveFlow.Application.Nodes.Definitions;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// Represents one node instance placed on the canvas.
/// It is driven by a NodeDefinition and contains visual state only.
/// </summary>
public partial class NodeInstanceViewModel : ObservableObject
{
    private const double NodeWidth = 230;
    private const double NodePortCenterY = 59;

    public string InstanceId { get; } = Guid.NewGuid().ToString("N");
    public NodeDefinition Definition { get; }

    public string Title => Definition.DisplayName;
    public string NodeType => Definition.NodeType;
    public string Category => Definition.Category.ToString();
    public string Subcategory => Definition.Subcategory;
    public string Description => Definition.Description;
    public string BadgeText => Definition.IsActionNode ? "ACTION" : "PREVIEW";
    public string ModeText => Definition.IsPreviewOnly ? "Preview only" : "Requires preview and apply";

    public int InputCount => Definition.InputPorts.Count;
    public int OutputCount => Definition.OutputPorts.Count;

    public bool HasInputPort => InputPort != null;
    public bool HasOutputPort => OutputPort != null;

    public IBrush AccentBrush { get; }
    public IBrush BadgeBrush { get; }
    public IBrush BorderBrush => IsSelected ? Brushes.White : AccentBrush;

    public PortInstanceViewModel? InputPort { get; }
    public PortInstanceViewModel? OutputPort { get; }

    public ObservableCollection<NodeParameterInstanceViewModel> Parameters { get; } = new();

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private string _status = "Ready";

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private int _lastInputFileCount;

    [ObservableProperty]
    private int _lastOutputFileCount;

    [ObservableProperty]
    private string _warningSummary = string.Empty;

    public NodeInstanceViewModel(NodeDefinition definition, double x, double y)
    {
        Definition = definition;
        X = x;
        Y = y;

        AccentBrush = Brush.Parse(definition.AccentColor);
        BadgeBrush = definition.IsActionNode
            ? Brush.Parse("#FF9800")
            : Brush.Parse("#4CAF50");

        foreach (var parameterDefinition in definition.Parameters)
        {
            Parameters.Add(new NodeParameterInstanceViewModel(parameterDefinition));
        }

        var inputDefinition = definition.InputPorts.FirstOrDefault();
        if (inputDefinition != null)
        {
            InputPort = new PortInstanceViewModel(
                this,
                isInput: true,
                inputDefinition.DataType,
                relativeX: 0,
                relativeY: NodePortCenterY);
        }

        var outputDefinition = definition.OutputPorts.FirstOrDefault();
        if (outputDefinition != null)
        {
            OutputPort = new PortInstanceViewModel(
                this,
                isInput: false,
                outputDefinition.DataType,
                relativeX: NodeWidth,
                relativeY: NodePortCenterY);
        }
    }

    public string GetParameterValue(string key, string defaultValue = "")
    {
        return Parameters.FirstOrDefault(x => x.Key == key)?.Value ?? defaultValue;
    }

    public string GetParameterSummary()
    {
        if (Parameters.Count == 0)
        {
            return "No parameters";
        }

        return string.Join(", ", Parameters.Take(3).Select(x => $"{x.DisplayName}: {x.Value}"));
    }

    public void SetRunStats(int inputCount, int outputCount)
    {
        LastInputFileCount = inputCount;
        LastOutputFileCount = outputCount;
    }

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(BorderBrush));
    }
}