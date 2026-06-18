using System;

namespace ArchiveFlow.Domain.Entities;

public class MetadataValue
{
    public int Id { get; set; }
    public string FileId { get; set; } = string.Empty;
    public int FieldId { get; set; }
    public string? ValueText { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // 方便 UI 顯示 (Join 後填入)
    public string FieldName { get; set; } = string.Empty;
}