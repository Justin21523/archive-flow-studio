using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Interfaces;

/// <summary>
/// Provides persistence operations for file records.
/// </summary>
public interface IFileRepository
{
    Task SaveAsync(FileRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FileRecord>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FileRecord?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<FileRecord?> GetByHashAsync(string fileHash, CancellationToken cancellationToken = default);
    Task UpdatePreviewAsync(FileRecord record, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
