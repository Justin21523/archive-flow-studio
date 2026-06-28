using ArchiveFlow.Application.DTOs;

namespace ArchiveFlow.Application.Interfaces;

public interface IExportService
{
    string GetPreviewPath(ExportFormat format, string requestedFileName);

    Task<ExportResult> ExportAsync(
        ExportRequest request,
        CancellationToken cancellationToken = default);
}
