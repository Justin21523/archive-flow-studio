namespace ArchiveFlow.Domain.Enums;

/// <summary>
/// Represents the processing status of a file in the archive system.
/// </summary>
public enum FileStatus
{
    /// <summary>File has been imported but not yet processed.</summary>
    New = 0,

    /// <summary>File is being scanned and analyzed.</summary>
    Scanning = 1,

    /// <summary>File has been scanned and metadata extracted.</summary>
    Scanned = 2,

    /// <summary>File has been organized and archived.</summary>
    Archived = 3,

    /// <summary>File is missing important metadata.</summary>
    Incomplete = 4,

    /// <summary>File is a duplicate of another file.</summary>
    Duplicate = 5,

    /// <summary>File has been marked for deletion.</summary>
    Deleted = 6
}