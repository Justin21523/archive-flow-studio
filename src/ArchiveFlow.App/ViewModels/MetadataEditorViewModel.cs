using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.App.ViewModels;

public partial class MetadataEditorViewModel : ObservableObject
{
    private readonly IMetadataRepository _metadataRepository;
    private readonly ILogger<MetadataEditorViewModel> _logger;
    private readonly string _fileId;

    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private string _fileId = string.Empty;
    [ObservableProperty] private string _statusMessage = "Ready";
    
    [ObservableProperty] private string _newFieldName = string.Empty;
    [ObservableProperty] private string _newFieldCategory = "Basic";
    [ObservableProperty] private string _newFieldValue = string.Empty;

    public ObservableCollection<MetadataFieldViewModel> BasicFields { get; } = new();
    public ObservableCollection<MetadataFieldViewModel> DescriptiveFields { get; } = new();
    public ObservableCollection<MetadataFieldViewModel> PersonalFields { get; } = new();
    public ObservableCollection<MetadataFieldViewModel> TechnicalFields { get; } = new();

    public MetadataEditorViewModel(
        IMetadataRepository metadataRepository,
        ILogger<MetadataEditorViewModel> logger,
        string fileId,
        string fileName)
    {
        _metadataRepository = metadataRepository;
        _logger = logger;
        _fileId = fileId;
        FileId = fileId;
        FileName = fileName;
        
        Task.Run(async () => await LoadMetadataAsync());
    }

    private async Task LoadMetadataAsync()
    {
        try
        {
            StatusMessage = "Loading metadata...";
            var metadata = await _metadataRepository.GetMetadataByFileIdAsync(_fileId);
            
            BasicFields.Clear();
            DescriptiveFields.Clear();
            PersonalFields.Clear();
            TechnicalFields.Clear();

            foreach (var meta in metadata)
            {
                var vm = new MetadataFieldViewModel(meta);
                
                switch (meta.Category.ToLowerInvariant())
                {
                    case "basic":
                        BasicFields.Add(vm);
                        break;
                    case "descriptive":
                        DescriptiveFields.Add(vm);
                        break;
                    case "personal":
                        PersonalFields.Add(vm);
                        break;
                    case "technical":
                        TechnicalFields.Add(vm);
                        break;
                    default:
                        BasicFields.Add(vm);
                        break;
                }
            }

            StatusMessage = $"Loaded {metadata.Count()} metadata fields";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load metadata");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            StatusMessage = "Saving changes...";
            
            // Save all modified fields
            var allFields = BasicFields.Concat(DescriptiveFields)
                                      .Concat(PersonalFields)
                                      .Concat(TechnicalFields);
            
            foreach (var field in allFields)
            {
                if (field.IsModified)
                {
                    var metaField = await _metadataRepository.GetOrCreateFieldAsync(
                        field.FieldName, field.FieldName, "String", field.Category);
                    
                    if (metaField != null)
                    {
                        await _metadataRepository.AddMetadataValueAsync(_fileId, metaField.Id, field.ValueText);
                        field.IsModified = false;
                    }
                }
            }

            StatusMessage = "Changes saved successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save metadata");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AddFieldAsync()
    {
        if (string.IsNullOrWhiteSpace(NewFieldName) || string.IsNullOrWhiteSpace(NewFieldValue))
        {
            StatusMessage = "Field name and value are required";
            return;
        }

        try
        {
            var metaField = await _metadataRepository.GetOrCreateFieldAsync(
                NewFieldName, NewFieldName, "String", NewFieldCategory);
            
            if (metaField != null)
            {
                await _metadataRepository.AddMetadataValueAsync(_fileId, metaField.Id, NewFieldValue);
                
                var newVm = new MetadataFieldViewModel(new MetadataValue
                {
                    FieldName = NewFieldName,
                    ValueText = NewFieldValue,
                    Category = NewFieldCategory
                });

                switch (NewFieldCategory.ToLowerInvariant())
                {
                    case "basic": BasicFields.Add(newVm); break;
                    case "descriptive": DescriptiveFields.Add(newVm); break;
                    case "personal": PersonalFields.Add(newVm); break;
                    case "technical": TechnicalFields.Add(newVm); break;
                }

                NewFieldName = string.Empty;
                NewFieldValue = string.Empty;
                StatusMessage = $"Added field: {NewFieldName}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add field");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RemoveField(MetadataFieldViewModel? field)
    {
        if (field == null) return;

        BasicFields.Remove(field);
        DescriptiveFields.Remove(field);
        PersonalFields.Remove(field);
        TechnicalFields.Remove(field);
        
        StatusMessage = $"Removed field: {field.FieldName}";
    }

    [RelayCommand]
    private void Close()
    {
        // Find the window and close it
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Close();
        }
    }
}

public partial class MetadataFieldViewModel : ObservableObject
{
    [ObservableProperty] private string _fieldName = string.Empty;
    [ObservableProperty] private string _valueText = string.Empty;
    [ObservableProperty] private string _category = string.Empty;
    [ObservableProperty] private bool _isModified;

    public MetadataFieldViewModel(MetadataValue meta)
    {
        FieldName = meta.FieldName;
        ValueText = meta.ValueText ?? string.Empty;
        Category = meta.Category;
    }

    partial void OnValueTextChanged(string value)
    {
        IsModified = true;
    }
}