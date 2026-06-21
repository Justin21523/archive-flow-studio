using ArchiveFlow.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// Represents one editable metadata row inside the standalone Metadata Editor.
/// </summary>
public partial class MetadataEditorFieldViewModel : ObservableObject
{
    public ObservableCollection<string> CategoryOptions { get; } = new()
    {
        "Basic",
        "Descriptive Metadata",
        "Personal Knowledge",
        "Technical Metadata"
    };

    public ObservableCollection<string> FieldTypeOptions { get; } = new()
    {
        "String",
        "LongText",
        "Number",
        "Date",
        "Boolean",
        "Uri"
    };

    public int MetadataValueId { get; set; }

    public int FieldId { get; set; }

    public string FileId { get; set; } = string.Empty;

    [ObservableProperty]
    private string _fieldName = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _fieldType = "String";

    [ObservableProperty]
    private string _category = "Basic";

    [ObservableProperty]
    private bool _isRequired;

    [ObservableProperty]
    private string _valueText = string.Empty;

    [ObservableProperty]
    private bool _isNew;

    [ObservableProperty]
    private bool _isDeleted;

    [ObservableProperty]
    private bool _isModified;

    public string RequiredBadge => IsRequired ? "Required" : "Optional";

    public MetadataEditorFieldViewModel()
    {
        IsNew = true;
        IsModified = true;
    }

    public MetadataEditorFieldViewModel(MetadataValue value)
    {
        MetadataValueId = value.Id;
        FieldId = value.FieldId;
        FileId = value.FileId;
        FieldName = value.FieldName;
        DisplayName = value.DisplayName;
        FieldType = string.IsNullOrWhiteSpace(value.FieldType) ? "String" : value.FieldType;
        Category = string.IsNullOrWhiteSpace(value.Category) ? "Basic" : value.Category;
        IsRequired = value.IsRequired;
        ValueText = value.ValueText;
        IsNew = false;
        IsModified = false;
    }

    public void MarkClean()
    {
        IsNew = false;
        IsModified = false;
    }

    partial void OnDisplayNameChanged(string value)
    {
        IsModified = true;
    }

    partial void OnFieldTypeChanged(string value)
    {
        IsModified = true;
    }

    partial void OnCategoryChanged(string value)
    {
        IsModified = true;
    }

    partial void OnIsRequiredChanged(bool value)
    {
        OnPropertyChanged(nameof(RequiredBadge));
        IsModified = true;
    }

    partial void OnValueTextChanged(string value)
    {
        IsModified = true;
    }
}