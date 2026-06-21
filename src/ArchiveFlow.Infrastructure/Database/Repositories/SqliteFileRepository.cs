using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using Dapper;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Infrastructure.Database.Repositories;

/// <summary>
/// SQLite implementation of file repository.
/// </summary>
public sealed class SqliteFileRepository : IFileRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly ILogger<SqliteFileRepository> _logger;

    public SqliteFileRepository(
        IDatabaseConnectionFactory connectionFactory,
        ILogger<SqliteFileRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task SaveAsync(FileRecord record, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO files (
                id,
                archive_id,
                file_path,
                file_name,
                file_extension,
                file_hash,
                file_size,
                mime_type,
                status,
                created_at,
                imported_at,
                modified_at,
                last_scanned_at,
                thumbnail_path,
                content_preview
            )
            VALUES (
                @Id,
                @ArchiveId,
                @FilePath,
                @FileName,
                @FileExtension,
                @FileHash,
                @FileSize,
                @MimeType,
                @Status,
                @CreatedAt,
                @ImportedAt,
                @ModifiedAt,
                @LastScannedAt,
                @ThumbnailPath,
                @ContentPreview
            )
            ON CONFLICT(id) DO UPDATE SET
                archive_id = excluded.archive_id,
                file_path = excluded.file_path,
                file_name = excluded.file_name,
                file_extension = excluded.file_extension,
                file_hash = excluded.file_hash,
                file_size = excluded.file_size,
                mime_type = excluded.mime_type,
                status = excluded.status,
                modified_at = excluded.modified_at,
                last_scanned_at = excluded.last_scanned_at,
                thumbnail_path = excluded.thumbnail_path,
                content_preview = excluded.content_preview;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, record);

        _logger.LogDebug("Saved file record {ArchiveId}", record.ArchiveId);
    }

    public async Task<IReadOnlyList<FileRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                archive_id AS ArchiveId,
                file_path AS FilePath,
                file_name AS FileName,
                file_extension AS FileExtension,
                file_hash AS FileHash,
                file_size AS FileSize,
                mime_type AS MimeType,
                status AS Status,
                created_at AS CreatedAt,
                imported_at AS ImportedAt,
                modified_at AS ModifiedAt,
                last_scanned_at AS LastScannedAt,
                thumbnail_path AS ThumbnailPath,
                content_preview AS ContentPreview
            FROM files
            ORDER BY imported_at DESC;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var records = await connection.QueryAsync<FileRecord>(sql);

        return records.ToList();
    }

    public async Task<FileRecord?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                archive_id AS ArchiveId,
                file_path AS FilePath,
                file_name AS FileName,
                file_extension AS FileExtension,
                file_hash AS FileHash,
                file_size AS FileSize,
                mime_type AS MimeType,
                status AS Status,
                created_at AS CreatedAt,
                imported_at AS ImportedAt,
                modified_at AS ModifiedAt,
                last_scanned_at AS LastScannedAt,
                thumbnail_path AS ThumbnailPath,
                content_preview AS ContentPreview
            FROM files
            WHERE id = @Id
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<FileRecord>(sql, new { Id = id });
    }

    public async Task<FileRecord?> GetByHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                archive_id AS ArchiveId,
                file_path AS FilePath,
                file_name AS FileName,
                file_extension AS FileExtension,
                file_hash AS FileHash,
                file_size AS FileSize,
                mime_type AS MimeType,
                status AS Status,
                created_at AS CreatedAt,
                imported_at AS ImportedAt,
                modified_at AS ModifiedAt,
                last_scanned_at AS LastScannedAt,
                thumbnail_path AS ThumbnailPath,
                content_preview AS ContentPreview
            FROM files
            WHERE file_hash = @FileHash
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<FileRecord>(sql, new { FileHash = fileHash });
    }

    public async Task UpdatePreviewAsync(FileRecord record, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE files
            SET
                thumbnail_path = @ThumbnailPath,
                content_preview = @ContentPreview
            WHERE id = @Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, record);

        _logger.LogDebug("Updated preview for file record {ArchiveId}", record.ArchiveId);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM files;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql);
    }
}
