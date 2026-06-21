using System.Collections.Generic;
using ArchiveFlow.Application.Nodes.Definitions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// Represents a parameter value for one node instance on the canvas.
/// </summary>
public partial class NodeParameterInstanceViewModel : ObservableObject
{
    public NodeParameterDefinition Definition { get; }

    public string Key => Definition.Key;
    public string DisplayName => Definition.DisplayName;
    public NodeParameterControlType ControlType => Definition.ControlType;
    public string ControlTypeLabel => Definition.ControlType.ToString();
    public bool IsRequired => Definition.IsRequired;
    public IReadOnlyList<string> Options => Definition.Options;

    [ObservableProperty]
    private string _value = string.Empty;

    public NodeParameterInstanceViewModel(NodeParameterDefinition definition)
    {
        Definition = definition;
        Value = definition.DefaultValue;
    }
}