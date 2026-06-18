using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Infrastructure.Search;

/// <summary>
/// Implementation of full-text search using SQLite FTS5.
/// </summary>
public class FullTextSearchService : IFullTextSearchService
{
    private readonly string _connectionString;
    private readonly ILogger<FullTextSearchService> _logger;

    public FullTextSearchService(ILogger<FullTextSearchService> logger)
    {
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "archiveflow.db");
        _connectionString = $"Data Source={dbPath};";
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public async Task<IEnumerable<FileRecord>> SearchAsync(string query, int maxResults = 100)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Enumerable.Empty<FileRecord>();
        }

        _logger.LogInformation("Performing full-text search for: {Query}", query);

        using var connection = CreateConnection();
        
        // Use FTS5 MATCH for full-text search
        // Supports boolean operators: AND, OR, NOT, phrase matching with ""
        const string sql = @"
            SELECT f.* 
            FROM files f
            INNER JOIN files_fts fts ON f.id = fts.file_id
            WHERE files_fts MATCH @Query
            ORDER BY rank
            LIMIT @MaxResults";

        var results = await connection.QueryAsync<FileRecord>(sql, new 
        { 
            Query = query,
            MaxResults = maxResults 
        });

        var resultList = results.ToList();
        _logger.LogInformation("Search returned {Count} results", resultList.Count);
        
        return resultList;
    }

    public async Task<IEnumerable<FileRecord>> AdvancedSearchAsync(
        string? keyword = null,
        string? fileType = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int maxResults = 100)
    {
        using var connection = CreateConnection();
        
        var sql = @"
            SELECT DISTINCT f.* 
            FROM files f
            LEFT JOIN files_fts fts ON f.id = fts.file_id
            WHERE 1=1";

        var parameters = new DynamicParameters();
        parameters.Add("MaxResults", maxResults);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            sql += " AND (files_fts MATCH @Keyword OR f.file_name LIKE @KeywordLike)";
            parameters.Add("Keyword", keyword);
            parameters.Add("KeywordLike", $"%{keyword}%");
        }

        if (!string.IsNullOrWhiteSpace(fileType))
        {
            sql += " AND f.file_extension = @FileType";
            parameters.Add("FileType", fileType.ToLowerInvariant());
        }

        if (dateFrom.HasValue)
        {
            sql += " AND f.created_at >= @DateFrom";
            parameters.Add("DateFrom", dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            sql += " AND f.created_at <= @DateTo";
            parameters.Add("DateTo", dateTo.Value);
        }

        sql += " ORDER BY f.created_at DESC LIMIT @MaxResults";

        var results = await connection.QueryAsync<FileRecord>(sql, parameters);
        return results.ToList();
    }

    public async Task RebuildIndexAsync()
    {
        _logger.LogInformation("Rebuilding full-text search index...");

        using var connection = CreateConnection();
        
        // Clear existing FTS index
        await connection.ExecuteAsync("DELETE FROM files_fts");

        // Re-populate from files table
        const string sql = @"
            INSERT INTO files_fts (file_id, file_name, file_path, content_preview)
            SELECT id, file_name, file_path, content_preview
            FROM files
            WHERE content_preview IS NOT NULL OR file_name IS NOT NULL";

        var count = await connection.ExecuteAsync(sql);
        
        _logger.LogInformation("Rebuilt FTS index with {Count} documents", count);
    }
}