using System;
using System.Collections.Generic;
using ArchiveFlow.Application.Nodes.Definitions;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// Represents a parameter value for one node instance on the canvas.
/// The Inspector renders controls dynamically based on ControlType.
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

    public bool IsTextLike =>
        ControlType == NodeParameterControlType.Text ||
        ControlType == NodeParameterControlType.Date;

    public bool IsNumber => ControlType == NodeParameterControlType.Number;
    public bool IsBoolean => ControlType == NodeParameterControlType.Boolean;
    public bool IsDropdown => ControlType == NodeParameterControlType.Dropdown;

    public string RequiredBadge => IsRequired ? "Required" : "Optional";

    public decimal NumericValue
    {
        get
        {
            return decimal.TryParse(Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0m;
        }
        set
        {
            Value = value.ToString(CultureInfo.InvariantCulture);
        }
    }

    public bool BooleanValue
    {
        get
        {
            return bool.TryParse(Value, out var result) && result;
        }
        set
        {
            Value = value.ToString();
        }
    }

    [ObservableProperty]
    private string _value = string.Empty;

    public event EventHandler? ValueChanged;

    public NodeParameterInstanceViewModel(NodeParameterDefinition definition)
    {
        Definition = definition;
        Value = definition.DefaultValue;
    }

    partial void OnValueChanged(string value)
    {
        OnPropertyChanged(nameof(NumericValue));
        OnPropertyChanged(nameof(BooleanValue));

        ValueChanged?.Invoke(this, EventArgs.Empty);
    }
}