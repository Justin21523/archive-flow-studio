using System;

namespace ArchiveFlow.Domain.Entities;

public class MetadataValue
{
    public int Id { get; set; }
    public string FileId { get; set; } = string.Empty;
    public int FieldId { get; set; }
    public string? ValueText { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Joined properties from MetadataField
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}