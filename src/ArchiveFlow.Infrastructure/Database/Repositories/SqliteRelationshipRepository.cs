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

public class SqliteRelationshipRepository : IRelationshipRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteRelationshipRepository> _logger;

    public SqliteRelationshipRepository(ILogger<SqliteRelationshipRepository> logger)
    {
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "archiveflow.db");
        _connectionString = $"Data Source={dbPath};";
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public async Task CreateRelationshipAsync(string sourceId, string targetId, string relationType)
    {
        using var connection = CreateConnection();
        const string sql = @"
            INSERT INTO file_relationships (source_file_id, target_file_id, relationship_type, created_at)
            VALUES (@SourceId, @TargetId, @RelationType, @CreatedAt)";
        
        await connection.ExecuteAsync(sql, new 
        { 
            SourceId = sourceId, 
            TargetId = targetId, 
            RelationType = relationType, 
            CreatedAt = DateTime.UtcNow 
        });
    }

    public async Task<IEnumerable<FileRelationship>> GetRelationshipsByFileIdAsync(string fileId)
    {
        using var connection = CreateConnection();
        const string sql = @"
            SELECT r.*, 
                   f1.file_name as SourceFileName, 
                   f2.file_name as TargetFileName
            FROM file_relationships r
            LEFT JOIN files f1 ON r.source_file_id = f1.id
            LEFT JOIN files f2 ON r.target_file_id = f2.id
            WHERE r.source_file_id = @FileId OR r.target_file_id = @FileId";
        
        return await connection.QueryAsync<FileRelationship>(sql, new { FileId = fileId });
    }

    public async Task<IEnumerable<FileRelationship>> GetAllRelationshipsAsync()
    {
        using var connection = CreateConnection();
        const string sql = @"
            SELECT r.*, 
                   f1.file_name as SourceFileName, 
                   f2.file_name as TargetFileName
            FROM file_relationships r
            LEFT JOIN files f1 ON r.source_file_id = f1.id
            LEFT JOIN files f2 ON r.target_file_id = f2.id";
        
        return await connection.QueryAsync<FileRelationship>(sql);
    }
}