namespace ArchiveFlow.Domain.Entities;

public sealed class ImportJobRecord
{
    public string Id { get; set; } = string.Empty;

    public string FolderPath { get; set; } = string.Empty;

    public bool Recursive { get; set; }

    public int TotalCount { get; set; }

    public int NewCount { get; set; }

    public int DuplicateCount { get; set; }

    public int ExistingCount { get; set; }

    public int ImportedCount { get; set; }

    public int ErrorCount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public static ImportJobRecord Create(
        string folderPath,
        bool recursive,
        int totalCount,
        int newCount,
        int duplicateCount,
        int existingCount,
        int importedCount,
        int errorCount,
        string status,
        string message,
        DateTime? startedAt = null,
        DateTime? finishedAt = null)
    {
        var now = DateTime.UtcNow;

        return new ImportJobRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            FolderPath = folderPath,
            Recursive = recursive,
            TotalCount = totalCount,
            NewCount = newCount,
            DuplicateCount = duplicateCount,
            ExistingCount = existingCount,
            ImportedCount = importedCount,
            ErrorCount = errorCount,
            Status = status,
            Message = message,
            StartedAt = startedAt ?? now,
            FinishedAt = finishedAt
        };
    }
}
