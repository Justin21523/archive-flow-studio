namespace ArchiveFlow.Application.Nodes;

/// <summary>
/// Represents the preview result of an Action Node before it is applied.
/// </summary>
public class ActionPreview
{
    public int AffectedFileCount { get; set; }
    public string Description { get; set; } = string.Empty;
}