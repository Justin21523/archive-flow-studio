namespace ArchiveFlow.Application.Interfaces;

/// <summary>
/// Provides safe read-only file system interactions for the desktop UI.
/// These operations must not modify, move, rename, or delete physical files.
/// </summary>
public interface IFileSystemInteractionService
{
    Task OpenFileAsync(string filePath, CancellationToken cancellationToken = default);

    Task RevealInFileExplorerAsync(string filePath, CancellationToken cancellationToken = default);
}