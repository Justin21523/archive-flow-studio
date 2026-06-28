using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Interfaces;

public interface IExportJobRepository
{
    Task SaveAsync(ExportJobRecord job, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExportJobRecord>> GetRecentAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);
}
