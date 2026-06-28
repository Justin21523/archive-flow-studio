using ArchiveFlow.Application.DTOs;

namespace ArchiveFlow.Application.Interfaces;

public interface IImportPipelineService
{
    Task<ImportPreviewResult> PreviewFolderAsync(
        string folderPath,
        bool recursive,
        CancellationToken cancellationToken = default);

    Task<ImportApplyResult> ApplyImportAsync(
        ImportPreviewResult preview,
        CancellationToken cancellationToken = default);
}
