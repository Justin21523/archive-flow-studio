using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Interfaces;

public interface IMetadataRepository
{
    Task<MetadataField?> GetOrCreateFieldAsync(
        string fieldName,
        string displayName,
        string fieldType = "String",
        string category = "Basic",
        bool isRequired = false);
    Task AddMetadataValueAsync(string fileId, int fieldId, string valueText);
    Task<IEnumerable<MetadataValue>> GetMetadataByFileIdAsync(string fileId);
    Task<IEnumerable<MetadataField>> GetAllFieldsAsync();
}
