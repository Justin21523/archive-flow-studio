using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Interfaces;

public interface IFileScanner
{
    IAsyncEnumerable<FileRecord> ScanDirectoryAsync(string directoryPath, bool recursive = true, CancellationToken cancellationToken = default);
}
