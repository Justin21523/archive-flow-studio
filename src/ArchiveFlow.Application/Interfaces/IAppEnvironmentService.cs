namespace ArchiveFlow.Application.Interfaces;

public interface IAppEnvironmentService
{
    string AppName { get; }

    bool IsBrowser { get; }

    bool IsDemoMode { get; }

    string StorageDescription { get; }
}
