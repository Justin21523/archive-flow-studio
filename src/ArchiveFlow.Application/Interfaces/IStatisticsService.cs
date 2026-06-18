using System.Threading.Tasks;
using ArchiveFlow.Application.DTOs;

namespace ArchiveFlow.Application.Interfaces;

/// <summary>
/// Service responsible for calculating archive statistics for the Dashboard.
/// </summary>
public interface IStatisticsService
{
    Task<StatisticsDto> GetArchiveStatisticsAsync();
    Task<DashboardStatisticsDto> GetDashboardStatisticsAsync();
}
