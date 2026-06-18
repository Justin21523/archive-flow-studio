using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Infrastructure.Database.Repositories;

public class SqliteSmartCollectionRepository : ISmartCollectionRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteSmartCollectionRepository> _logger;

    public SqliteSmartCollectionRepository(ILogger<SqliteSmartCollectionRepository> logger)
    {
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "archiveflow.db");
        _connectionString = $"Data Source={dbPath};";
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public async Task CreateAsync(SmartCollection collection)
    {
        using var connection = CreateConnection();
        const string sql = @"
            INSERT INTO smart_collections (name, filter_rule_json, created_at) 
            VALUES (@Name, @FilterRuleJson, @CreatedAt)";
        await connection.ExecuteAsync(sql, collection);
    }

    public async Task<IEnumerable<SmartCollection>> GetAllAsync()
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<SmartCollection>("SELECT * FROM smart_collections ORDER BY created_at DESC");
    }

    public async Task<SmartCollection?> GetByNameAsync(string name)
    {
        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<SmartCollection>(
            "SELECT * FROM smart_collections WHERE name = @Name", new { Name = name });
    }
}