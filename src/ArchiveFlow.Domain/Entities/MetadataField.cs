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
    public string FieldType { get; set; } = "String"; // String, Integer, Date, Boolean
    
    /// <summary>
    /// Category used for grouping in the Metadata Editor UI.
    /// Examples: "Basic", "Descriptive", "Personal", "Technical"
    /// </summary>
    public string Category { get; set; } = "Basic";

    /// <summary>
    /// Indicates if this field is required for metadata completeness calculation.
    /// </summary>
    public bool IsRequired { get; set; } = false;
}