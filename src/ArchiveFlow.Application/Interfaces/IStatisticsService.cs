using System.Threading.Tasks;
using ArchiveFlow.Application.DTOs;

namespace ArchiveFlow.Application.Interfaces;

/// <summary>
/// Service responsible for calculating archive statistics and health metrics.
/// </summary>
public interface IStatisticsService
{
    Task<StatisticsDto> GetArchiveStatisticsAsync();
}