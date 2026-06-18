using FluentMigrator;

namespace ArchiveFlow.Infrastructure.Database.Migrations;

/// <summary>
/// Rebuilds the FTS table created by the older single-content-column migration.
/// </summary>
[Migration(202606190001, "Rebuild full-text search table with file metadata columns")]
public class _008_RebuildFullTextSearchTable : Migration
{
    public override void Up()
    {
        Execute.Sql("DROP TRIGGER IF EXISTS files_fts_delete;");
        Execute.Sql("DROP TRIGGER IF EXISTS files_fts_update;");
        Execute.Sql("DROP TRIGGER IF EXISTS files_fts_insert;");
        Execute.Sql("DROP TABLE IF EXISTS files_fts;");

        Execute.Sql(@"
            CREATE VIRTUAL TABLE files_fts USING fts5(
                file_id UNINDEXED,
                file_name,
                file_path,
                content_preview,
                tokenize='porter unicode61'
            );
        ");

        Execute.Sql(@"
            INSERT INTO files_fts (file_id, file_name, file_path, content_preview)
            SELECT id, file_name, file_path, content_preview
            FROM files;
        ");

        Execute.Sql(@"
            CREATE TRIGGER files_fts_insert AFTER INSERT ON files
            BEGIN
                INSERT INTO files_fts (file_id, file_name, file_path, content_preview)
                VALUES (new.id, new.file_name, new.file_path, new.content_preview);
            END;
        ");

        Execute.Sql(@"
            CREATE TRIGGER files_fts_update AFTER UPDATE ON files
            BEGIN
                UPDATE files_fts SET
                    file_name = new.file_name,
                    file_path = new.file_path,
                    content_preview = new.content_preview
                WHERE file_id = new.id;
            END;
        ");

        Execute.Sql(@"
            CREATE TRIGGER files_fts_delete AFTER DELETE ON files
            BEGIN
                DELETE FROM files_fts WHERE file_id = old.id;
            END;
        ");
    }

    public override void Down()
    {
        Execute.Sql("DROP TRIGGER IF EXISTS files_fts_delete;");
        Execute.Sql("DROP TRIGGER IF EXISTS files_fts_update;");
        Execute.Sql("DROP TRIGGER IF EXISTS files_fts_insert;");
        Execute.Sql("DROP TABLE IF EXISTS files_fts;");

        Execute.Sql("CREATE VIRTUAL TABLE files_fts USING fts5(file_id, content);");
    }
}
