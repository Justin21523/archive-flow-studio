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

public class SqliteSearchService : ISearchService
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteSearchService> _logger;

    public SqliteSearchService(ILogger<SqliteSearchService> logger)
    {
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "archiveflow.db");
        _connectionString = $"Data Source={dbPath};";
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public async Task IndexFileAsync(FileRecord file, string additionalContent = "")
    {
        using var connection = CreateConnection();
        // 組合搜尋內容：檔名 + 路徑 + 額外內容
        string content = $"{file.FileName} {file.FilePath} {additionalContent}";
        
        // 先刪除舊的索引，再插入新的
        await connection.ExecuteAsync("DELETE FROM files_fts WHERE file_id = @FileId", new { FileId = file.Id });
        await connection.ExecuteAsync("INSERT INTO files_fts (file_id, content) VALUES (@FileId, @Content)", 
            new { FileId = file.Id, Content = content });
    }

    public async Task<IEnumerable<FileRecord>> SearchAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return Enumerable.Empty<FileRecord>();

        using var connection = CreateConnection();
        // FTS5 使用 MATCH 語法進行全文搜尋
        const string sql = @"
            SELECT f.* FROM files f
            JOIN files_fts fts ON f.id = fts.file_id
            WHERE files_fts MATCH @Keyword";
        
        return await connection.QueryAsync<FileRecord>(sql, new { Keyword = keyword });
    }
}