using FluentMigrator;

namespace ArchiveFlow.Infrastructure.Database.Migrations;

[Migration(202606180003, "Create FTS5 virtual table for full-text search")]
public class _003_CreateFtsTable : Migration
{
    public override void Up()
    {
        // 使用 Execute.Sql 建立 SQLite 原生的 FTS5 虛擬表
        // 我們索引 file_id 以及一個合併的 content 欄位 (包含檔名、路徑、Metadata)
        Execute.Sql("CREATE VIRTUAL TABLE IF NOT EXISTS files_fts USING fts5(file_id, content);");
    }

    public override void Down()
    {
        Execute.Sql("DROP TABLE IF EXISTS files_fts;");
    }
}