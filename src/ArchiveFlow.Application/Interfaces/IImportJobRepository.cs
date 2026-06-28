using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Interfaces;

public interface IImportJobRepository
{
    Task SaveAsync(ImportJobRecord job, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImportJobRecord>> GetRecentAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);
}
