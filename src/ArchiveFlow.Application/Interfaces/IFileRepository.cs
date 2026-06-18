using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Interfaces;

public interface IFileRepository
{
    Task SaveAsync(FileRecord record, CancellationToken cancellationToken = default);
    Task<IEnumerable<FileRecord>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FileRecord?> GetByHashAsync(string fileHash, CancellationToken cancellationToken = default);
    Task UpdatePreviewAsync(FileRecord record);
}
