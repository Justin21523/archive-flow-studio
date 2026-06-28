namespace ArchiveFlow.Application.Interfaces;

public interface IDemoDataService
{
    Task ResetAsync(CancellationToken cancellationToken = default);

    Task LoadScenarioAsync(string scenarioName, CancellationToken cancellationToken = default);
}
