namespace ArchiveFlow.Application.Interfaces;

public interface INotificationService
{
    Task ShowInfoAsync(string message, CancellationToken cancellationToken = default);

    Task ShowWarningAsync(string message, CancellationToken cancellationToken = default);

    Task ShowErrorAsync(string message, CancellationToken cancellationToken = default);
}
