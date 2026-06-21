namespace ArchiveFlow.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a file record in the archive.
/// </summary>
public enum FileStatus
{
    New = 0,
    Scanning = 1,
    Scanned = 2,
    Archived = 3,
    Incomplete = 4,
    Duplicate = 5,
    Missing = 6,
    Deleted = 7
}