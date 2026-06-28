using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.DTOs;

public enum ImportPreviewStatus
{
    New,
    Duplicate,
    Existing,
    Failed
}

public sealed class ImportPreviewItem
{
    public FileRecord? FileRecord { get; init; }

    public string FilePath { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string FileExtension { get; init; } = string.Empty;

    public long FileSize { get; init; }

    public string FileHash { get; init; } = string.Empty;

    public ImportPreviewStatus Status { get; init; }

    public string ExistingArchiveId { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public bool CanImport => Status == ImportPreviewStatus.New && FileRecord != null;
}

public sealed class ImportPreviewResult
{
    public string JobId { get; init; } = string.Empty;

    public string FolderPath { get; init; } = string.Empty;

    public bool Recursive { get; init; }

    public IReadOnlyList<ImportPreviewItem> Items { get; init; } = Array.Empty<ImportPreviewItem>();

    public int NewCount => Items.Count(item => item.Status == ImportPreviewStatus.New);

    public int DuplicateCount => Items.Count(item => item.Status == ImportPreviewStatus.Duplicate);

    public int ExistingCount => Items.Count(item => item.Status == ImportPreviewStatus.Existing);

    public int FailedCount => Items.Count(item => item.Status == ImportPreviewStatus.Failed);

    public int TotalCount => Items.Count;
}

public sealed class ImportApplyResult
{
    public string JobId { get; init; } = string.Empty;

    public int ImportedCount { get; init; }

    public int SkippedCount { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public string Summary =>
        $"Imported {ImportedCount} files. Skipped {SkippedCount}. Errors {Errors.Count}. Warnings {Warnings.Count}.";
}
