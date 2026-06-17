using System.IO;
using ArchiveFlow.Domain.Enums;

namespace ArchiveFlow.Domain.Entities;

/// <summary>
/// Represents a file record in the archive system.
/// This is the core entity that tracks all managed files.
/// </summary>
public class FileRecord
{
    public Guid Id { get; private set; }
    public string ArchiveId { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string FileExtension { get; private set; } = string.Empty;
    public string FileHash { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string MimeType { get; private set; } = string.Empty;
    public FileStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ImportedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }
    public DateTime? LastScannedAt { get; private set; }

    private FileRecord() { } // For ORM

    public static FileRecord Create(
        string filePath,
        string fileHash,
        long fileSize,
        string mimeType)
    {
        var record = new FileRecord
        {
            Id = Guid.NewGuid(),
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            FileExtension = Path.GetExtension(filePath).ToLowerInvariant(),
            FileHash = fileHash,
            FileSize = fileSize,
            MimeType = mimeType,
            Status = FileStatus.New,
            CreatedAt = DateTime.UtcNow,
            ImportedAt = DateTime.UtcNow,
            ModifiedAt = File.GetLastWriteTimeUtc(filePath)
        };

        return record;
    }

    public void MarkAsScanned()
    {
        LastScannedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(FileStatus newStatus)
    {
        Status = newStatus;
    }

    public void UpdatePath(string newPath)
    {
        FilePath = newPath;
        FileName = Path.GetFileName(newPath);
        ModifiedAt = DateTime.UtcNow;
    }
}