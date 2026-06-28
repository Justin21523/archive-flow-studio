namespace ArchiveFlow.Domain.Entities;

public sealed class ExportJobRecord
{
    public string Id { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public int FileCount { get; set; }

    public string OutputPath { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public static ExportJobRecord Create(
        string format,
        int fileCount,
        string outputPath,
        string status,
        string message)
    {
        return new ExportJobRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Format = format,
            FileCount = fileCount,
            OutputPath = outputPath,
            Status = status,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };
    }
}
