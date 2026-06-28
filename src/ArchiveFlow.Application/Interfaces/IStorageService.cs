namespace ArchiveFlow.Application.Interfaces;

public interface IStorageService
{
    Task SaveTextAsync(string key, string value, CancellationToken cancellationToken = default);

    Task<string?> LoadTextAsync(string key, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
