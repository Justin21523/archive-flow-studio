using System.Collections.ObjectModel;
using ArchiveFlow.Application.Nodes.Definitions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// Represents a configurable parameter for a node (e.g., Dropdown, Number, Text).
/// </summary>
public partial class NodeParameterViewModel : ObservableObject
{
    public string Key { get; }
    [ObservableProperty] private string _label = string.Empty;
    [ObservableProperty] private string _value = string.Empty;
    [ObservableProperty] private string _type = "Text"; // Text, Number, Dropdown, Toggle

    // For Dropdown type
    public ObservableCollection<string> Options { get; } = new();

    public NodeParameterViewModel(string label, string type, string defaultValue = "")
    {
        Key = label;
        Label = label;
        Type = type;
        Value = defaultValue;
    }

    public NodeParameterViewModel(ParameterDefinition definition)
    {
        Key = definition.Key;
        Label = definition.Label;
        Type = definition.Type;
        Value = definition.DefaultValue;

        foreach (var option in definition.Options)
        {
            Options.Add(option);
        }
    }
}
