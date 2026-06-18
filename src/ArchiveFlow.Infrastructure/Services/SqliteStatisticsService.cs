using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArchiveFlow.Application.DTOs;
using ArchiveFlow.Application.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Infrastructure.Services;

/// <summary>
/// Calculates statistics directly from the SQLite database.
/// </summary>
public class SqliteStatisticsService : IStatisticsService
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteStatisticsService> _logger;

    public SqliteStatisticsService(ILogger<SqliteStatisticsService> logger)
    {
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "archiveflow.db");
        _connectionString = $"Data Source={dbPath};";
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public async Task<StatisticsDto> GetArchiveStatisticsAsync()
    {
        var stats = new StatisticsDto();

        using var connection = CreateConnection();
        
        // 1. Get Total Files and Size
        var fileStats = await connection.QueryFirstOrDefaultAsync<FileStatsRow>(
            "SELECT COUNT(*) as Count, COALESCE(SUM(file_size), 0) as TotalSize FROM files"
        ) ?? new FileStatsRow();
        
        stats.TotalFiles = fileStats.Count;
        stats.TotalSizeBytes = fileStats.TotalSize;

        // 2. Get Total Metadata Entries
        stats.TotalMetadataEntries = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM metadata_values"
        );

        // 3. Calculate Metadata Completeness
        if (stats.TotalFiles > 0)
        {
            var filesWithMetadata = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(DISTINCT file_id) FROM metadata_values"
            );
            stats.MetadataCompleteness = (filesWithMetadata / (double)stats.TotalFiles) * 100;
        }

        // 4. Get File Type Distribution
        var distribution = await connection.QueryAsync<(string Ext, int Count)>(
            "SELECT file_extension as Ext, COUNT(*) as Count FROM files GROUP BY file_extension ORDER BY Count DESC LIMIT 5"
        );

        int maxCount = distribution.Any() ? distribution.Max(x => x.Count) : 1;

        foreach (var item in distribution)
        {
            stats.FileTypeDistribution[item.Ext] = item.Count;
        }

        return stats;
    }

    public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync()
    {
        var archiveStats = await GetArchiveStatisticsAsync();

        return new DashboardStatisticsDto
        {
            TotalFiles = archiveStats.TotalFiles,
            TotalSizeBytes = archiveStats.TotalSizeBytes,
            TotalMetadataEntries = archiveStats.TotalMetadataEntries,
            MetadataCompleteness = archiveStats.MetadataCompleteness,
            FileTypeDistribution = new Dictionary<string, int>(archiveStats.FileTypeDistribution)
        };
    }

    private sealed class FileStatsRow
    {
        public int Count { get; set; }
        public long TotalSize { get; set; }
    }
}
