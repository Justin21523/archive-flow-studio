using Dapper;
using Microsoft.Extensions.Logging; 
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Infrastructure.Database.Repositories;

public class SqliteMetadataRepository : IMetadataRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly ILogger<SqliteMetadataRepository> _logger;
    
    public SqliteMetadataRepository(
        IDatabaseConnectionFactory connectionFactory,
        ILogger<SqliteMetadataRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<MetadataField> GetOrCreateFieldAsync(
        string fieldName,
        string displayName,
        string fieldType,
        string category,
        bool isRequired = false,
        CancellationToken cancellationToken = default)
    {
        fieldName = NormalizeFieldName(fieldName);

        const string selectSql = """
            SELECT
                id AS Id,
                field_name AS FieldName,
                display_name AS DisplayName,
                field_type AS FieldType,
                category AS Category,
                is_required AS IsRequired,
                sort_order AS SortOrder
            FROM metadata_fields
            WHERE field_name = @FieldName
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<MetadataField>(
            selectSql,
            new { FieldName = fieldName });

        if (existing != null)
        {
            return existing;
        }

        const string insertSql = """
            INSERT INTO metadata_fields (
                field_name,
                display_name,
                field_type,
                category,
                is_required,
                sort_order
            )
            VALUES (
                @FieldName,
                @DisplayName,
                @FieldType,
                @Category,
                @IsRequired,
                @SortOrder
            );
            """;


        await connection.ExecuteAsync(insertSql, new
        {
            FieldName = fieldName,
            DisplayName = displayName,
            FieldType = fieldType,
            Category = category,
            IsRequired = isRequired,
            SortOrder = 0
        });

        _logger.LogInformation("Created metadata field {FieldName}", fieldName);

        var created = await connection.QueryFirstAsync<MetadataField>(
            selectSql,
            new { FieldName = fieldName });

        return created;
    }

    public async Task<IReadOnlyList<MetadataField>> GetAllFieldsAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                field_name AS FieldName,
                display_name AS DisplayName,
                field_type AS FieldType,
                category AS Category,
                is_required AS IsRequired,
                sort_order AS SortOrder
            FROM metadata_fields
            ORDER BY category, sort_order, display_name;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var fields = await connection.QueryAsync<MetadataField>(sql);

        return fields.ToList();
    }

    public async Task<MetadataValue> AddMetadataValueAsync(
        string fileId,
        int fieldId,
        string valueText,
        CancellationToken cancellationToken = default)
    {
        const string insertSql = """
            INSERT INTO metadata_values (
                file_id,
                field_id,
                value_text,
                created_at,
                updated_at
            )
            VALUES (
                @FileId,
                @FieldId,
                @ValueText,
                @CreatedAt,
                @UpdatedAt
            );
            """;

        using var connection = _connectionFactory.CreateConnection();

        var now = DateTime.UtcNow;

        await connection.ExecuteAsync(insertSql, new
        {
            FileId = fileId,
            FieldId = fieldId,
            ValueText = valueText,
            CreatedAt = now,
            UpdatedAt = now
        });

        const string selectSql = """
            SELECT
                mv.id AS Id,
                mv.file_id AS FileId,
                mv.field_id AS FieldId,
                COALESCE(mv.value_text, '') AS ValueText,
                mv.created_at AS CreatedAt,
                mv.updated_at AS UpdatedAt,
                mf.field_name AS FieldName,
                mf.display_name AS DisplayName,
                mf.field_type AS FieldType,
                mf.category AS Category,
                mf.is_required AS IsRequired,
                mf.sort_order AS SortOrder
            FROM metadata_values mv
            INNER JOIN metadata_fields mf ON mv.field_id = mf.id
            WHERE mv.file_id = @FileId
              AND mv.field_id = @FieldId
            ORDER BY mv.id DESC
            LIMIT 1;
            """;

        return await connection.QueryFirstAsync<MetadataValue>(
            selectSql,
            new
            {
                FileId = fileId,
                FieldId = fieldId
            });
    }
  
    public async Task<IReadOnlyList<MetadataValue>> GetMetadataByFileIdAsync(
        string fileId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                mv.id AS Id,
                mv.file_id AS FileId,
                mv.field_id AS FieldId,
                COALESCE(mv.value_text, '') AS ValueText,
                mv.created_at AS CreatedAt,
                mv.updated_at AS UpdatedAt,
                mf.field_name AS FieldName,
                mf.display_name AS DisplayName,
                mf.field_type AS FieldType,
                mf.category AS Category,
                mf.is_required AS IsRequired
            FROM metadata_values mv
            INNER JOIN metadata_fields mf ON mv.field_id = mf.id
            WHERE mv.file_id = @FileId
            ORDER BY mf.category, mf.sort_order, mf.display_name;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var values = await connection.QueryAsync<MetadataValue>(sql, new { FileId = fileId });

        return values.ToList();
    }
    public async Task UpdateMetadataValueAsync(
        int metadataValueId,
        string valueText,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE metadata_values
            SET value_text = @ValueText,
                updated_at = @UpdatedAt
            WHERE id = @MetadataValueId;
            """;

        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(sql, new
        {
            MetadataValueId = metadataValueId,
            ValueText = valueText,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task DeleteMetadataValueByIdAsync(
        int metadataValueId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM metadata_values
            WHERE id = @MetadataValueId;
            """;

        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(sql, new { MetadataValueId = metadataValueId });
    }

    public async Task SetMetadataValueAsync(
        string fileId,
        string fieldName,
        string displayName,
        string fieldType,
        string category,
        string valueText,
        bool isRequired = false,
        CancellationToken cancellationToken = default)
    {
        var field = await GetOrCreateFieldAsync(
            fieldName,
            displayName,
            fieldType,
            category,
            isRequired,
            cancellationToken);

        const string findSql = """
            SELECT id
            FROM metadata_values
            WHERE file_id = @FileId
              AND field_id = @FieldId
            ORDER BY created_at
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateConnection();

        var existingId = await connection.QueryFirstOrDefaultAsync<int?>(
            findSql,
            new
            {
                FileId = fileId,
                FieldId = field.Id
            });

        if (existingId.HasValue)
        {
            await UpdateMetadataValueAsync(existingId.Value, valueText, cancellationToken);
            return;
        }

        await AddMetadataValueAsync(fileId, field.Id, valueText, cancellationToken);
    }

    public async Task AddMetadataValueIfMissingAsync(
        string fileId,
        string fieldName,
        string displayName,
        string fieldType,
        string category,
        string valueText,
        bool isRequired = false,
        CancellationToken cancellationToken = default)
    {
        var exists = await HasMetadataValueAsync(
            fileId,
            fieldName,
            valueText,
            cancellationToken);

        if (exists)
        {
            return;
        }

        var field = await GetOrCreateFieldAsync(
            fieldName,
            displayName,
            fieldType,
            category,
            isRequired,
            cancellationToken);

        await AddMetadataValueAsync(fileId, field.Id, valueText, cancellationToken);
    }

    public async Task DeleteMetadataValueAsync(
        string fileId,
        string fieldName,
        string? valueText = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM metadata_values
            WHERE file_id = @FileId
              AND field_id = (
                  SELECT id
                  FROM metadata_fields
                  WHERE field_name = @FieldName
                  LIMIT 1
              )
              AND (
                  @ValueText IS NULL
                  OR value_text = @ValueText
              );
            """;

        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(sql, new
        {
            FileId = fileId,
            FieldName = NormalizeFieldName(fieldName),
            ValueText = valueText
        });
    }

    private static string NormalizeFieldName(string fieldName)
    {
        return fieldName
            .Trim()
            .Replace(" ", "_")
            .Replace("-", "_")
            .ToLowerInvariant();
    }

    private static string NormalizeFieldType(string fieldType)
    {
        if (string.IsNullOrWhiteSpace(fieldType))
        {
            return "String";
        }

        return fieldType.Trim();
    }

    private static string NormalizeCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return "Basic";
        }

        return category.Trim();
    }

    private static string ToDisplayName(string fieldName)
    {
        return string.Join(
            " ",
            fieldName
                .Replace("_", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }

    public async Task<IReadOnlyList<MetadataValue>> GetMetadataValuesByFieldAsync(
        string fileId,
        string fieldName,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                mv.id AS Id,
                mv.file_id AS FileId,
                mv.field_id AS FieldId,
                COALESCE(mv.value_text, '') AS ValueText,
                mv.created_at AS CreatedAt,
                mv.updated_at AS UpdatedAt,
                mf.field_name AS FieldName,
                mf.display_name AS DisplayName,
                mf.field_type AS FieldType,
                mf.category AS Category,
                mf.is_required AS IsRequired
            FROM metadata_values mv
            INNER JOIN metadata_fields mf ON mv.field_id = mf.id
            WHERE mv.file_id = @FileId
              AND mf.field_name = @FieldName
            ORDER BY mv.created_at;
            """;

        using var connection = _connectionFactory.CreateConnection();

        var values = await connection.QueryAsync<MetadataValue>(
            sql,
            new
            {
                FileId = fileId,
                FieldName = NormalizeFieldName(fieldName)
            });

        return values.ToList();
    }            
    public async Task<string?> GetFirstMetadataValueAsync(
        string fileId,
        string fieldName,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT mv.value_text
            FROM metadata_values mv
            INNER JOIN metadata_fields mf ON mv.field_id = mf.id
            WHERE mv.file_id = @FileId
              AND mf.field_name = @FieldName
            ORDER BY mv.created_at
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<string?>(
            sql,
            new
            {
                FileId = fileId,
                FieldName = NormalizeFieldName(fieldName)
            });
    }

    public async Task<bool> HasMetadataValueAsync(
        string fileId,
        string fieldName,
        string valueText,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM metadata_values mv
            INNER JOIN metadata_fields mf ON mv.field_id = mf.id
            WHERE mv.file_id = @FileId
              AND mf.field_name = @FieldName
              AND LOWER(COALESCE(mv.value_text, '')) = LOWER(@ValueText);
            """;

        using var connection = _connectionFactory.CreateConnection();

        var count = await connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                FileId = fileId,
                FieldName = NormalizeFieldName(fieldName),
                ValueText = valueText
            });

        return count > 0;
    }

}
