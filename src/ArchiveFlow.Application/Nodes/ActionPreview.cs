namespace ArchiveFlow.Application.Nodes;

/// <summary>
/// Represents the preview result of an Action Node before it is applied.
/// </summary>
public class ActionPreview
{
    public string NodeName { get; set; } = string.Empty;
    public int AffectedFileCount { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsDangerous { get; set; } = false; // e.g., Delete file
}