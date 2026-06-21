using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Interfaces;

/// <summary>
/// Provides persistence operations for dynamic metadata fields and values.
/// </summary>
public interface IMetadataRepository
{
    Task<MetadataField> GetOrCreateFieldAsync(
        string fieldName,
        string displayName,
        string fieldType,
        string category,
        bool isRequired = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MetadataField>> GetAllFieldsAsync(
        CancellationToken cancellationToken = default);

    Task AddMetadataValueAsync(
        string fileId,
        int fieldId,
        string valueText,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MetadataValue>> GetMetadataByFileIdAsync(
        string fileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MetadataValue>> GetMetadataValuesByFieldAsync(
        string fileId,
        string fieldName,
        CancellationToken cancellationToken = default);

    Task<string?> GetFirstMetadataValueAsync(
        string fileId,
        string fieldName,
        CancellationToken cancellationToken = default);

    Task<bool> HasMetadataValueAsync(
        string fileId,
        string fieldName,
        string valueText,
        CancellationToken cancellationToken = default);

    Task SetMetadataValueAsync(
        string fileId,
        string fieldName,
        string displayName,
        string fieldType,
        string category,
        string valueText,
        bool isRequired = false,
        CancellationToken cancellationToken = default);

    Task AddMetadataValueIfMissingAsync(
        string fileId,
        string fieldName,
        string displayName,
        string fieldType,
        string category,
        string valueText,
        bool isRequired = false,
        CancellationToken cancellationToken = default);

    Task DeleteMetadataValueAsync(
        string fileId,
        string fieldName,
        string? valueText = null,
        CancellationToken cancellationToken = default);
}
