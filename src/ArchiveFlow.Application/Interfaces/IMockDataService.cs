using System.Threading.Tasks;

namespace ArchiveFlow.Application.Interfaces;

/// <summary>
/// Service to generate mock files and database records for testing and demonstration.
/// </summary>
public interface IMockDataService
{
    Task GenerateMockDataAsync();
}