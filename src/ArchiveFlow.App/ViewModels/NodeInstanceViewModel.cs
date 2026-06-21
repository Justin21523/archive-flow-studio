using System;
using System.Threading.Tasks;
using ArchiveFlow.Application.Nodes.Definitions;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// Represents one node instance placed on the canvas.
/// </summary>
public partial class NodeInstanceViewModel : ObservableObject
{
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
    public IBrush AccentBrush { get; }
    public IBrush BadgeBrush { get; }
    public IBrush BorderBrush => IsSelected ? Brushes.White : AccentBrush;

    public ObservableCollection<NodeParameterInstanceViewModel> Parameters { get; } = new();

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private string _status = "Ready";

    [ObservableProperty]
    private bool _isSelected;

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
    }

    public string GetParameterSummary()
    {
        if (Parameters.Count == 0)
        {
            return "No parameters";
        }

        return string.Join(", ", Parameters.Take(2).Select(x => $"{x.DisplayName}: {x.Value}"));
    }

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(BorderBrush));
    }
}