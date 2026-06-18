namespace ArchiveFlow.Domain.Entities;

/// <summary>
/// Represents a dynamic collection of files based on a specific filter rule.
/// Unlike static collections, members are determined at runtime by evaluating the rule.
/// </summary>
public class SmartCollection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON string representing the filter rule (e.g., {"Field": "extension", "Value": ".pdf"}).
    /// </summary>
    public string FilterRuleJson { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}