namespace ArchiveFlow.Application.Interfaces;

public interface IFilePickerService
{
    Task<string?> PickFolderAsync(CancellationToken cancellationToken = default);

    Task<string?> PickSaveFileAsync(string suggestedFileName, CancellationToken cancellationToken = default);
}
