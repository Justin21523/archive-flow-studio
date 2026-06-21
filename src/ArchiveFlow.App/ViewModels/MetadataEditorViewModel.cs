using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.App.ViewModels;

public partial class MetadataEditorViewModel : ObservableObject
{
    private readonly IMetadataRepository _metadataRepository;
    private readonly ILogger<MetadataEditorViewModel> _logger;
    private readonly FileRecord _fileRecord;
    private readonly List<MetadataEditorFieldViewModel> _deletedFields = new();
    
    public event EventHandler? RequestClose;
    
    [ObservableProperty] private string _statusMessage = "Ready";
    
    [ObservableProperty] private string _newFieldName = string.Empty;
    [ObservableProperty] private string _newFieldCategory = "Basic";
    [ObservableProperty] private string _newFieldValue = string.Empty;

    public ObservableCollection<MetadataEditorFieldViewModel> BasicFields { get; } = new();
    public ObservableCollection<MetadataEditorFieldViewModel> DescriptiveFields { get; } = new();
    public ObservableCollection<MetadataEditorFieldViewModel> PersonalFields { get; } = new();
    public ObservableCollection<MetadataEditorFieldViewModel> TechnicalFields { get; } = new();

    public ObservableCollection<string> NewFieldCategoryOptions { get; } = new()
    {
        "Basic",
        "Descriptive Metadata",
        "Personal Knowledge",
        "Technical Metadata"
    };

    public ObservableCollection<string> NewFieldTypeOptions { get; } = new()
    {
        "String",
        "LongText",
        "Number",
        "Date",
        "Boolean",
        "Uri"
    };

    public string FileName => _fileRecord.FileName;
    public string FileId => _fileRecord.Id;
    public string ArchiveId => _fileRecord.ArchiveId;
    public string FilePath => _fileRecord.FilePath;
    public string FileExtension => _fileRecord.FileExtension;
    public string ImportedAtText => _fileRecord.ImportedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    [ObservableProperty]
    private string _metadataCompletenessText = "0%";

    [ObservableProperty]
    private double _metadataCompleteness;


    [ObservableProperty]
    private string _newDisplayName = string.Empty;

    [ObservableProperty]
    private string _newFieldType = "String";

    [ObservableProperty]
    private bool _newIsRequired;

    [ObservableProperty]
    private string _newValueText = string.Empty;

    public MetadataEditorViewModel(
        IMetadataRepository metadataRepository,
        ILogger<MetadataEditorViewModel> logger,
        FileRecord fileRecord)
    {
        _metadataRepository = metadataRepository;
        _logger = logger;
        _fileRecord = fileRecord;
    }

    public async Task InitializeAsync()
    {
        await LoadMetadataAsync();
    }

    [RelayCommand]
    private async Task ReloadAsync()
    {
        await LoadMetadataAsync();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        StatusMessage = "Saving metadata...";

        try
        {
            foreach (var deletedField in _deletedFields.ToList())
            {
                if (deletedField.MetadataValueId > 0)
                {
                    await _metadataRepository.DeleteMetadataValueByIdAsync(deletedField.MetadataValueId);
                }

                _deletedFields.Remove(deletedField);
            }

            foreach (var field in GetAllVisibleFields().Where(x => x.IsNew || x.IsModified))
            {
                if (field.IsNew)
                {
                    var metadataField = await _metadataRepository.GetOrCreateFieldAsync(
                        field.FieldName,
                        field.DisplayName,
                        field.FieldType,
                        field.Category,
                        field.IsRequired);

                    var created = await _metadataRepository.AddMetadataValueAsync(
                        _fileRecord.Id,
                        metadataField.Id,
                        field.ValueText);

                    field.MetadataValueId = created.Id;
                    field.FieldId = created.FieldId;
                    field.FileId = created.FileId;
                    field.FieldName = created.FieldName;
                    field.DisplayName = created.DisplayName;
                    field.FieldType = created.FieldType;
                    field.Category = created.Category;
                    field.IsRequired = created.IsRequired;
                    field.ValueText = created.ValueText;
                    field.MarkClean();

                    continue;
                }

                if (field.FieldId > 0)
                {
                    await _metadataRepository.UpdateFieldDefinitionAsync(
                        field.FieldId,
                        field.DisplayName,
                        field.FieldType,
                        field.Category,
                        field.IsRequired);
                }

                if (field.MetadataValueId > 0)
                {
                    await _metadataRepository.UpdateMetadataValueAsync(
                        field.MetadataValueId,
                        field.ValueText);
                }

                field.MarkClean();
            }

            await LoadMetadataAsync();

            StatusMessage = "Metadata saved.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save metadata for file {FileId}", _fileRecord.Id);
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }

    public void RemoveField(MetadataEditorFieldViewModel? field)
    {
        if (field == null)
        {
            return;
        }

        RemoveFromVisibleCollections(field);

        if (!field.IsNew)
        {
            field.IsDeleted = true;
            _deletedFields.Add(field);
        }

        StatusMessage = $"Marked '{field.DisplayName}' for deletion. Click Save to apply.";
        _ = RecalculateCompletenessAsync();
    }

    [RelayCommand]
    private void AddField()
    {
        if (string.IsNullOrWhiteSpace(NewFieldName))
        {
            StatusMessage = "Field name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewValueText))
        {
            StatusMessage = "Value is required.";
            return;
        }

        var displayName = string.IsNullOrWhiteSpace(NewDisplayName)
            ? ToDisplayName(NewFieldName)
            : NewDisplayName.Trim();

        var field = new MetadataEditorFieldViewModel
        {
            FieldName = NormalizeFieldName(NewFieldName),
            DisplayName = displayName,
            FieldType = NewFieldType,
            Category = NewFieldCategory,
            IsRequired = NewIsRequired,
            ValueText = NewValueText,
            FileId = _fileRecord.Id,
            IsNew = true,
            IsModified = true
        };

        AddToCategory(field);

        NewFieldName = string.Empty;
        NewDisplayName = string.Empty;
        NewValueText = string.Empty;
        NewFieldCategory = "Personal Knowledge";
        NewFieldType = "String";
        NewIsRequired = false;

        StatusMessage = $"Added '{field.DisplayName}'. Click Save to write it to SQLite.";
        _ = RecalculateCompletenessAsync();
    }

    [RelayCommand]
    private async Task SaveAndCloseAsync()
    {
        await SaveAsync();
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }


    private async Task EnsureBasicReadOnlyMetadataAsync()
    {
        if (BasicFields.Any(x => x.FieldName == "archive_id"))
        {
            return;
        }

        BasicFields.Insert(0, new MetadataEditorFieldViewModel
        {
            FieldName = "archive_id",
            DisplayName = "Archive ID",
            FieldType = "String",
            Category = "Basic",
            IsRequired = true,
            ValueText = _fileRecord.ArchiveId,
            FileId = _fileRecord.Id,
            IsNew = true,
            IsModified = true
        });

        BasicFields.Insert(1, new MetadataEditorFieldViewModel
        {
            FieldName = "original_filename",
            DisplayName = "Original Filename",
            FieldType = "String",
            Category = "Basic",
            IsRequired = true,
            ValueText = _fileRecord.FileName,
            FileId = _fileRecord.Id,
            IsNew = true,
            IsModified = true
        });

        await Task.CompletedTask;
    }

    private async Task RecalculateCompletenessAsync()
    {
        var allDefinitions = await _metadataRepository.GetAllFieldsAsync();
        var requiredFields = allDefinitions.Where(x => x.IsRequired).ToList();
        var visibleFields = GetAllVisibleFields().Where(x => !x.IsDeleted).ToList();

        if (requiredFields.Count > 0)
        {
            var presentRequired = requiredFields.Count(definition =>
                visibleFields.Any(value =>
                    string.Equals(value.FieldName, definition.FieldName, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(value.ValueText)));

            MetadataCompleteness = presentRequired / (double)requiredFields.Count * 100.0;
        }
        else
        {
            var nonEmpty = visibleFields.Count(x => !string.IsNullOrWhiteSpace(x.ValueText));
            MetadataCompleteness = visibleFields.Count == 0
                ? 0
                : nonEmpty / (double)visibleFields.Count * 100.0;
        }

        MetadataCompletenessText = $"{MetadataCompleteness:F0}%";
    }

    private IEnumerable<MetadataEditorFieldViewModel> GetAllVisibleFields()
    {
        return BasicFields
            .Concat(DescriptiveFields)
            .Concat(PersonalFields)
            .Concat(TechnicalFields);
    }

    private void AddToCategory(MetadataEditorFieldViewModel field)
    {
        switch (NormalizeCategoryForSwitch(field.Category))
        {
            case "basic":
                BasicFields.Add(field);
                break;

            case "descriptive":
            case "descriptive metadata":
                DescriptiveFields.Add(field);
                break;

            case "personal":
            case "personal knowledge":
                PersonalFields.Add(field);
                break;

            case "technical":
            case "technical metadata":
                TechnicalFields.Add(field);
                break;

            default:
                BasicFields.Add(field);
                break;
        }
    }

    private void RemoveFromVisibleCollections(MetadataEditorFieldViewModel field)
    {
        BasicFields.Remove(field);
        DescriptiveFields.Remove(field);
        PersonalFields.Remove(field);
        TechnicalFields.Remove(field);
    }

    private static string NormalizeFieldName(string value)
    {
        return value
            .Trim()
            .Replace(" ", "_")
            .Replace("-", "_")
            .ToLowerInvariant();
    }

    private static string NormalizeCategoryForSwitch(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static string ToDisplayName(string value)
    {
        var normalized = NormalizeFieldName(value).Replace("_", " ");

        return string.Join(
            " ",
            normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }
    private async Task LoadMetadataAsync()
    {
        StatusMessage = "Loading metadata...";

        try
        {
            BasicFields.Clear();
            DescriptiveFields.Clear();
            PersonalFields.Clear();
            TechnicalFields.Clear();
            _deletedFields.Clear();

            var values = await _metadataRepository.GetMetadataByFileIdAsync(_fileRecord.Id);

            foreach (var value in values)
            {
                AddToCategory(new MetadataEditorFieldViewModel(value));
            }

            await EnsureBasicReadOnlyMetadataAsync();
            await RecalculateCompletenessAsync();

            StatusMessage = $"Loaded {values.Count} metadata values.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load metadata for file {FileId}", _fileRecord.Id);
            StatusMessage = $"Load failed: {ex.Message}";
        }
    }

}
