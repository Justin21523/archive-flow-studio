namespace ArchiveFlow.Domain.Entities;

public class MetadataField
{
    public int Id { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FieldType { get; set; } = "String";
}