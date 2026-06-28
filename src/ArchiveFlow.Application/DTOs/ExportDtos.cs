using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.DTOs;

public enum ExportFormat
{
    Csv,
    Json,
    DublinCoreXml
}

public sealed class ExportRequest
{
    public ExportFormat Format { get; init; }

    public IReadOnlyList<FileRecord> Files { get; init; } = Array.Empty<FileRecord>();

    public string RequestedFileName { get; init; } = string.Empty;
}

public sealed class ExportResult
{
    public string JobId { get; init; } = string.Empty;

    public bool Success { get; init; }

    public ExportFormat Format { get; init; }

    public int FileCount { get; init; }

    public string OutputPath { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}
