using System;
using System.IO;
using ArchiveFlow.Domain.Enums;

namespace ArchiveFlow.Domain.Entities;

public class FileRecord
{
    // 使用 string 來存儲 Guid，避免 Dapper 解析錯誤
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
    public string? ThumbnailPath { get; set; }
    public string? ContentPreview { get; set; }
    
    // Parameterless constructor for Dapper
    public FileRecord() { }

    public static FileRecord Create(string filePath, string fileHash, long fileSize, string mimeType)
    {
        return new FileRecord
        {
            Id = Guid.NewGuid().ToString(),
            ArchiveId = GenerateArchiveId(),
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            FileExtension = Path.GetExtension(filePath).ToLowerInvariant(),
            FileHash = fileHash,
            FileSize = fileSize,
            MimeType = mimeType,
            Status = (int)FileStatus.New,
            CreatedAt = DateTime.UtcNow,
            ImportedAt = DateTime.UtcNow,
            ModifiedAt = File.GetLastWriteTimeUtc(filePath)
        };
    }

    public void UpdateStatus(FileStatus newStatus)
    {
        Status = (int)newStatus;
    }

    public void UpdateFileSize(long fileSize)
    {
        if (fileSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSize), "File size cannot be negative.");
        }

        FileSize = fileSize;
    }

    public void UpdatePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));
        }

        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        FileExtension = Path.GetExtension(filePath).ToLowerInvariant();
        ModifiedAt = File.Exists(filePath) ? File.GetLastWriteTimeUtc(filePath) : DateTime.UtcNow;
    }

    public FileStatus GetStatus()
    {
        return (FileStatus)Status;
    }

    private static string GenerateArchiveId()
    {
        return $"AFA-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}
