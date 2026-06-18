using System;

namespace ArchiveFlow.Domain.Entities;

/// <summary>
/// Represents a directed relationship between two files in the archive.
/// </summary>
public class FileRelationship
{
    public int Id { get; set; }
    public string SourceFileId { get; set; } = string.Empty;
    public string TargetFileId { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty; // e.g., "HasNote", "UsesAsset", "References"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties for UI display
    public string SourceFileName { get; set; } = string.Empty;
    public string TargetFileName { get; set; } = string.Empty;
}