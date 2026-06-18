using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// Represents a configurable parameter for a node (e.g., Dropdown, Number, Text).
/// </summary>
public partial class NodeParameterViewModel : ObservableObject
{
    [ObservableProperty] private string _label = string.Empty;
    [ObservableProperty] private string _value = string.Empty;
    [ObservableProperty] private string _type = "Text"; // Text, Number, Dropdown, Toggle

    // For Dropdown type
    public ObservableCollection<string> Options { get; } = new();

    public NodeParameterViewModel(string label, string type, string defaultValue = "")
    {
        Label = label;
        Type = type;
        Value = defaultValue;
    }
}