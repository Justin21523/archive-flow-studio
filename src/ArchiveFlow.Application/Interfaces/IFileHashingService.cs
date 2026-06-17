namespace ArchiveFlow.Application.Interfaces;

public interface IFileHashingService
{
    Task<string> ComputeSha256HashAsync(string filePath, CancellationToken cancellationToken = default);
}
