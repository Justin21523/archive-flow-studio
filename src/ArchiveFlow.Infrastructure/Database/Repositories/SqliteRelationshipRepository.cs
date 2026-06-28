using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using Dapper;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Infrastructure.Database.Repositories;

public class SqliteRelationshipRepository : IRelationshipRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly ILogger<SqliteRelationshipRepository> _logger;

    public SqliteRelationshipRepository(
        IDatabaseConnectionFactory connectionFactory,
        ILogger<SqliteRelationshipRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task CreateRelationshipAsync(string sourceId, string targetId, string relationType)
    {
        await TryCreateRelationshipAsync(sourceId, targetId, relationType);
    }

    public async Task<bool> TryCreateRelationshipAsync(string sourceId, string targetId, string relationType)
    {
        using var connection = _connectionFactory.CreateConnection();
        relationType = relationType.Trim();
        if (string.IsNullOrWhiteSpace(sourceId) ||
            string.IsNullOrWhiteSpace(targetId) ||
            string.IsNullOrWhiteSpace(relationType))
        {
            return false;
        }

        const string sql = @"
            INSERT OR IGNORE INTO file_relationships (source_file_id, target_file_id, relationship_type, created_at)
            VALUES (@SourceId, @TargetId, @RelationType, @CreatedAt)";
        
        var rowsAffected = await connection.ExecuteAsync(sql, new
        { 
            SourceId = sourceId, 
            TargetId = targetId, 
            RelationType = relationType, 
            CreatedAt = DateTime.UtcNow 
        });

        return rowsAffected > 0;
    }

    public async Task<bool> RelationshipExistsAsync(string sourceId, string targetId, string relationType)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT COUNT(1)
            FROM file_relationships
            WHERE source_file_id = @SourceId
              AND target_file_id = @TargetId
              AND relationship_type = @RelationType";

        var count = await connection.ExecuteScalarAsync<int>(sql, new
        {
            SourceId = sourceId,
            TargetId = targetId,
            RelationType = relationType.Trim()
        });

        return count > 0;
    }

    public async Task<bool> UpdateRelationshipTypeAsync(int id, string relationType)
    {
        relationType = relationType.Trim();
        if (string.IsNullOrWhiteSpace(relationType))
        {
            return false;
        }

        using var connection = _connectionFactory.CreateConnection();
        var current = await GetByIdAsync(connection, id);
        if (current == null)
        {
            return false;
        }

        if (current.RelationshipType.Equals(relationType, StringComparison.Ordinal))
        {
            return true;
        }

        const string duplicateSql = @"
            SELECT COUNT(1)
            FROM file_relationships
            WHERE id <> @Id
              AND source_file_id = @SourceFileId
              AND target_file_id = @TargetFileId
              AND relationship_type = @RelationshipType";
        var duplicateCount = await connection.ExecuteScalarAsync<int>(duplicateSql, new
        {
            Id = id,
            current.SourceFileId,
            current.TargetFileId,
            RelationshipType = relationType
        });

        if (duplicateCount > 0)
        {
            return false;
        }

        const string updateSql = @"
            UPDATE file_relationships
            SET relationship_type = @RelationshipType
            WHERE id = @Id";
        var rowsAffected = await connection.ExecuteAsync(updateSql, new
        {
            Id = id,
            RelationshipType = relationType
        });

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteRelationshipAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var rowsAffected = await connection.ExecuteAsync(
            "DELETE FROM file_relationships WHERE id = @Id",
            new { Id = id });

        return rowsAffected > 0;
    }

    public async Task<IEnumerable<FileRelationship>> GetRelationshipsByFileIdAsync(string fileId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT
                   r.id AS Id,
                   r.source_file_id AS SourceFileId,
                   r.target_file_id AS TargetFileId,
                   r.relationship_type AS RelationshipType,
                   r.created_at AS CreatedAt,
                   f1.file_name AS SourceFileName, 
                   f2.file_name AS TargetFileName
            FROM file_relationships r
            LEFT JOIN files f1 ON r.source_file_id = f1.id
            LEFT JOIN files f2 ON r.target_file_id = f2.id
            WHERE r.source_file_id = @FileId OR r.target_file_id = @FileId";
        
        return await connection.QueryAsync<FileRelationship>(sql, new { FileId = fileId });
    }

    public async Task<IEnumerable<FileRelationship>> GetAllRelationshipsAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT
                   r.id AS Id,
                   r.source_file_id AS SourceFileId,
                   r.target_file_id AS TargetFileId,
                   r.relationship_type AS RelationshipType,
                   r.created_at AS CreatedAt,
                   f1.file_name AS SourceFileName, 
                   f2.file_name AS TargetFileName
            FROM file_relationships r
            LEFT JOIN files f1 ON r.source_file_id = f1.id
            LEFT JOIN files f2 ON r.target_file_id = f2.id";
        
        return await connection.QueryAsync<FileRelationship>(sql);
    }

    private static async Task<FileRelationship?> GetByIdAsync(
        System.Data.IDbConnection connection,
        int id)
    {
        const string sql = @"
            SELECT
                   id AS Id,
                   source_file_id AS SourceFileId,
                   target_file_id AS TargetFileId,
                   relationship_type AS RelationshipType,
                   created_at AS CreatedAt
            FROM file_relationships
            WHERE id = @Id";

        return await connection.QueryFirstOrDefaultAsync<FileRelationship>(sql, new { Id = id });
    }
}
