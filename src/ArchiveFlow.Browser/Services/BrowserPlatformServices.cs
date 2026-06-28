using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Browser.Services;

public sealed class BrowserAppEnvironmentService : IAppEnvironmentService
{
    public string AppName => "ArchiveFlow Studio Browser Demo";

    public bool IsBrowser => true;

    public bool IsDemoMode => true;

    public string StorageDescription => "In-memory browser demo storage";
}

public sealed class BrowserMemoryStorageService : IStorageService
{
    private readonly Dictionary<string, string> _storage = new(StringComparer.Ordinal);

    public Task SaveTextAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        _storage[key] = value;
        return Task.CompletedTask;
    }

    public Task<string?> LoadTextAsync(string key, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _storage.Remove(key);
        return Task.CompletedTask;
    }
}

public sealed class BrowserDemoFilePickerService : IFilePickerService
{
    public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>("browser-demo://sample-import");
    }

    public Task<string?> PickSaveFileAsync(string suggestedFileName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>($"browser-download://{suggestedFileName}");
    }
}

public sealed class BrowserDemoNotificationService : INotificationService
{
    public string LastMessage { get; private set; } = string.Empty;

    public Task ShowInfoAsync(string message, CancellationToken cancellationToken = default)
    {
        LastMessage = message;
        return Task.CompletedTask;
    }

    public Task ShowWarningAsync(string message, CancellationToken cancellationToken = default)
    {
        LastMessage = message;
        return Task.CompletedTask;
    }

    public Task ShowErrorAsync(string message, CancellationToken cancellationToken = default)
    {
        LastMessage = message;
        return Task.CompletedTask;
    }
}
