namespace ArchiveFlow.Domain.Entities;

/// <summary>
/// Represents a definable metadata field (e.g., Title, Subject, Tag).
/// Supports categorization for UI grouping and template mapping.
/// </summary>
public class MetadataField
{
    public int Id { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string FieldType { get; set; } = "String";

    public string Category { get; set; } = "Basic";

    public bool IsRequired { get; set; }

    public int SortOrder { get; set; }
}