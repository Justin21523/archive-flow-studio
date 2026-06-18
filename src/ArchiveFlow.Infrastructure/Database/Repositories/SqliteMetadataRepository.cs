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

public class SqliteMetadataRepository : IMetadataRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteMetadataRepository> _logger;

    public SqliteMetadataRepository(ILogger<SqliteMetadataRepository> logger)
    {
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "archiveflow.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = $"Data Source={dbPath};";
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public async Task<MetadataField?> GetOrCreateFieldAsync(string fieldName, string displayName, string fieldType = "String", string category = "Basic", bool isRequired = false)
    {
        using var connection = CreateConnection();
        var field = await connection.QueryFirstOrDefaultAsync<MetadataField>(
            "SELECT * FROM metadata_fields WHERE field_name = @FieldName", 
            new { FieldName = fieldName });

        if (field != null) return field;

        await connection.ExecuteAsync(
            "INSERT INTO metadata_fields (field_name, display_name, field_type, category, is_required) VALUES (@FieldName, @DisplayName, @FieldType, @Category, @IsRequired)",
            new { FieldName = fieldName, DisplayName = displayName, FieldType = fieldType, Category = category, IsRequired = isRequired });

        return await connection.QueryFirstOrDefaultAsync<MetadataField>(
            "SELECT * FROM metadata_fields WHERE field_name = @FieldName", 
            new { FieldName = fieldName });
    }

    public async Task AddMetadataValueAsync(string fileId, int fieldId, string valueText)
    {
        using var connection = CreateConnection();
        // Prevent duplicate simple tags/subjects for the same file (Optional logic, simplified here)
        await connection.ExecuteAsync(
            "INSERT INTO metadata_values (file_id, field_id, value_text, created_at) VALUES (@FileId, @FieldId, @ValueText, @CreatedAt)",
            new { FileId = fileId, FieldId = fieldId, ValueText = valueText, CreatedAt = DateTime.UtcNow });
    }

    public async Task<IEnumerable<MetadataValue>> GetMetadataByFileIdAsync(string fileId)
    {
        using var connection = CreateConnection();
        const string sql = @"
            SELECT mv.id, mv.file_id as FileId, mv.field_id as FieldId, mv.value_text as ValueText, mv.created_at as CreatedAt, 
                   mf.field_name as FieldName, mf.display_name as DisplayName, mf.category as Category
            FROM metadata_values mv 
            JOIN metadata_fields mf ON mv.field_id = mf.id 
            WHERE mv.file_id = @FileId";
        return await connection.QueryAsync<MetadataValue>(sql, new { FileId = fileId });
    }

    public async Task<IEnumerable<MetadataField>> GetAllFieldsAsync()
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<MetadataField>("SELECT * FROM metadata_fields ORDER BY category, field_name");
    }
}