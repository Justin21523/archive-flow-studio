using System.Data;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Infrastructure.Database.Repositories;

public class SqliteFileRepository : IFileRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteFileRepository> _logger;

    public SqliteFileRepository(ILogger<SqliteFileRepository> logger)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ArchiveFlow",
            "archiveflow.db"
        );
        
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        
        _connectionString = $"Data Source={dbPath};";
        _logger = logger;
    }

    private IDbConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }

    public async Task SaveAsync(FileRecord record, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO files (id, archive_id, file_path, file_name, file_extension, file_hash, file_size, mime_type, status, created_at, imported_at, modified_at, last_scanned_at)
            VALUES (@Id, @ArchiveId, @FilePath, @FileName, @FileExtension, @FileHash, @FileSize, @MimeType, @Status, @CreatedAt, @ImportedAt, @ModifiedAt, @LastScannedAt)
            ON CONFLICT(id) DO UPDATE SET
                file_path = excluded.file_path,
                file_name = excluded.file_name,
                file_size = excluded.file_size,
                status = excluded.status,
                modified_at = excluded.modified_at,
                last_scanned_at = excluded.last_scanned_at;
        ";

        using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, record);
        _logger.LogDebug("Saved file record: {ArchiveId}", record.ArchiveId);
    }

    public async Task<IEnumerable<FileRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM files ORDER BY imported_at DESC;";
        using var connection = CreateConnection();
        return await connection.QueryAsync<FileRecord>(sql);
    }

    public async Task<FileRecord?> GetByHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM files WHERE file_hash = @FileHash LIMIT 1;";
        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<FileRecord>(sql, new { FileHash = fileHash });
    }
}
