using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Infrastructure.Database.Repositories;

public class SqliteMetadataRepository : IMetadataRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteMetadataRepository> _logger;

    public SqliteMetadataRepository(ILogger<SqliteMetadataRepository> logger)
    {
        var dbPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ArchiveFlow",
            "archiveflow.db"
        );
        _connectionString = $"Data Source={dbPath};";
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public async Task<MetadataField?> GetOrCreateFieldAsync(string fieldName, string displayName, string fieldType = "String")
    {
        using var connection = CreateConnection();
        var field = await connection.QueryFirstOrDefaultAsync<MetadataField>(
            "SELECT * FROM metadata_fields WHERE field_name = @FieldName", 
            new { FieldName = fieldName });

        if (field != null) return field;

        await connection.ExecuteAsync(
            "INSERT INTO metadata_fields (field_name, display_name, field_type) VALUES (@FieldName, @DisplayName, @FieldType)",
            new { FieldName = fieldName, DisplayName = displayName, FieldType = fieldType });

        return await connection.QueryFirstOrDefaultAsync<MetadataField>(
            "SELECT * FROM metadata_fields WHERE field_name = @FieldName", 
            new { FieldName = fieldName });
    }

    public async Task AddMetadataValueAsync(string fileId, int fieldId, string valueText)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync(
            "INSERT INTO metadata_values (file_id, field_id, value_text, created_at) VALUES (@FileId, @FieldId, @ValueText, @CreatedAt)",
            new { FileId = fileId, FieldId = fieldId, ValueText = valueText, CreatedAt = DateTime.UtcNow });
    }

    public async Task<IEnumerable<MetadataValue>> GetMetadataByFileIdAsync(string fileId)
    {
        using var connection = CreateConnection();
        const string sql = @"
            SELECT mv.id, mv.file_id as FileId, mv.field_id as FieldId, mv.value_text as ValueText, mv.created_at as CreatedAt, mf.field_name as FieldName
            FROM metadata_values mv
            JOIN metadata_fields mf ON mv.field_id = mf.id
            WHERE mv.file_id = @FileId";
        return await connection.QueryAsync<MetadataValue>(sql, new { FileId = fileId });
    }
}