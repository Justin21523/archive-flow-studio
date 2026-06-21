namespace ArchiveFlow.Domain.Entities;

/// <summary>
/// Represents one metadata value assigned to a file.
/// Joined field information is included for editor and inspector display.
/// </summary>
public sealed class MetadataValue
{
    public int Id { get; set; }

    public string FileId { get; set; } = string.Empty;

    public int FieldId { get; set; }

    public string ValueText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string FieldType { get; set; } = "String";

    public string Category { get; set; } = "Basic";

    public bool IsRequired { get; set; }

    public int SortOrder { get; set; }
}