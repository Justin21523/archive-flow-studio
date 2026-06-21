namespace ArchiveFlow.Application.DTOs;

/// <summary>
/// Represents the result of resetting and generating mock archive data.
/// </summary>
public sealed class MockArchiveSeedResult
{
    public int FileCount { get; set; }

    public int MetadataValueCount { get; set; }

    public int DuplicateGroupCount { get; set; }

    public string MockRootPath { get; set; } = string.Empty;

    public Dictionary<string, int> ExtensionCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
}