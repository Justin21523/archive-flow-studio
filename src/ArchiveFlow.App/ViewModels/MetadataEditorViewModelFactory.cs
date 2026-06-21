using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// Creates MetadataEditorViewModel instances for selected files.
/// </summary>
public sealed class MetadataEditorViewModelFactory
{
    private readonly IMetadataRepository _metadataRepository;
    private readonly ILogger<MetadataEditorViewModel> _logger;

    public MetadataEditorViewModelFactory(
        IMetadataRepository metadataRepository,
        ILogger<MetadataEditorViewModel> logger)
    {
        _metadataRepository = metadataRepository;
        _logger = logger;
    }

    public MetadataEditorViewModel Create(FileRecord fileRecord)
    {
        return new MetadataEditorViewModel(
            _metadataRepository,
            _logger,
            fileRecord);
    }
}