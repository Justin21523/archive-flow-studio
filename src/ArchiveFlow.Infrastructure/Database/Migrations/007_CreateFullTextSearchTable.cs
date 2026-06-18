using FluentMigrator;

namespace ArchiveFlow.Infrastructure.Database.Migrations;

/// <summary>
/// Migration to create SQLite FTS5 virtual table for full-text search.
/// FTS5 provides efficient full-text search capabilities.
/// </summary>
[Migration(202606190000, "Create FTS5 virtual table for full-text search")]
public class _007_CreateFullTextSearchTable : Migration
{
    public override void Up()
    {
        // Create FTS5 virtual table
        // This table will index file names, paths, and content previews
        Execute.Sql(@"
            CREATE VIRTUAL TABLE IF NOT EXISTS files_fts USING fts5(
                file_id,
                file_name,
                file_path,
                content_preview,
                tokenize='porter unicode61'
            );
        ");

        // Create trigger to auto-update FTS index on INSERT
        Execute.Sql(@"
            CREATE TRIGGER IF NOT EXISTS files_fts_insert AFTER INSERT ON files
            BEGIN
                INSERT INTO files_fts (file_id, file_name, file_path, content_preview)
                VALUES (new.id, new.file_name, new.file_path, new.content_preview);
            END;
        ");

        // Create trigger to auto-update FTS index on UPDATE
        Execute.Sql(@"
            CREATE TRIGGER IF NOT EXISTS files_fts_update AFTER UPDATE ON files
            BEGIN
                UPDATE files_fts SET
                    file_name = new.file_name,
                    file_path = new.file_path,
                    content_preview = new.content_preview
                WHERE file_id = new.id;
            END;
        ");

        // Create trigger to auto-update FTS index on DELETE
        Execute.Sql(@"
            CREATE TRIGGER IF NOT EXISTS files_fts_delete AFTER DELETE ON files
            BEGIN
                DELETE FROM files_fts WHERE file_id = old.id;
            END;
        ");
    }

    public override void Down()
    {
        Execute.Sql("DROP TRIGGER IF EXISTS files_fts_delete");
        Execute.Sql("DROP TRIGGER IF EXISTS files_fts_update");
        Execute.Sql("DROP TRIGGER IF EXISTS files_fts_insert");
        Execute.Sql("DROP TABLE IF EXISTS files_fts");
    }
}