using ArchiveFlow.Domain.Enums;

namespace ArchiveFlow.Domain.Entities;

/// <summary>
/// Represents a file tracked by ArchiveFlow Studio.
/// </summary>
public class FileRecord
{
    public string Id { get; private set; } = string.Empty;
    public string ArchiveId { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string FileExtension { get; private set; } = string.Empty;
    public string FileHash { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string MimeType { get; private set; } = string.Empty;
    public int Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ImportedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }
    public DateTime? LastScannedAt { get; private set; }

    public string ThumbnailPath { get; private set; } = string.Empty;
    public string ContentPreview { get; private set; } = string.Empty;

    public FileRecord()
    {
    }

    public static FileRecord Create(
        string filePath,
        string fileHash,
        long fileSize,
        string mimeType)
    {
        var now = DateTime.UtcNow;

        return new FileRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            ArchiveId = GenerateArchiveId(),
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            FileExtension = Path.GetExtension(filePath).ToLowerInvariant(),
            FileHash = fileHash,
            FileSize = fileSize,
            MimeType = mimeType,
            Status = (int)FileStatus.New,
            CreatedAt = now,
            ImportedAt = now,
            ModifiedAt = File.Exists(filePath) ? File.GetLastWriteTimeUtc(filePath) : now
        };
    }

    public void UpdateStatus(FileStatus newStatus)
    {
        Status = (int)newStatus;
    }

    public void MarkAsScanned()
    {
        LastScannedAt = DateTime.UtcNow;
        Status = (int)FileStatus.Scanned;
    }

    public void UpdatePath(string newPath)
    {
        FilePath = newPath;
        FileName = Path.GetFileName(newPath);
        FileExtension = Path.GetExtension(newPath).ToLowerInvariant();
        ModifiedAt = DateTime.UtcNow;
    }

    public void UpdatePreview(string contentPreview, string thumbnailPath = "")
    {
        ContentPreview = contentPreview;
        ThumbnailPath = thumbnailPath;
    }

    public void UpdateContentPreview(string contentPreview)
    {
        ContentPreview = contentPreview;
    }

    public void UpdateThumbnailPath(string thumbnailPath)
    {
        ThumbnailPath = thumbnailPath;
    }

    public void UpdateFileSize(long fileSize)
    {
        if (fileSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSize), "File size cannot be negative.");
        }

        FileSize = fileSize;
        ModifiedAt = DateTime.UtcNow;
    }

    public FileStatus GetStatus()
    {
        return Enum.IsDefined(typeof(FileStatus), Status)
            ? (FileStatus)Status
            : FileStatus.New;
    }

    private static string GenerateArchiveId()
    {
        return $"AF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }
}
