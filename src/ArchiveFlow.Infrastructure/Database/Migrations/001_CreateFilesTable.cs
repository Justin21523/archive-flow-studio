using FluentMigrator;

namespace ArchiveFlow.Infrastructure.Database.Migrations;

/// <summary>
/// Creates the core files table.
/// </summary>
[Migration(202606180001, "Create files table")]
public sealed class _001_CreateFilesTable : Migration
{
    public override void Up()
    {
        Create.Table("files")
            .WithColumn("id").AsString(32).PrimaryKey()
            .WithColumn("archive_id").AsString(64).NotNullable().Unique()
            .WithColumn("file_path").AsString(2000).NotNullable()
            .WithColumn("file_name").AsString(512).NotNullable()
            .WithColumn("file_extension").AsString(64).NotNullable()
            .WithColumn("file_hash").AsString(128).NotNullable()
            .WithColumn("file_size").AsInt64().NotNullable()
            .WithColumn("mime_type").AsString(128).NotNullable()
            .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("imported_at").AsDateTime().NotNullable()
            .WithColumn("modified_at").AsDateTime().Nullable()
            .WithColumn("last_scanned_at").AsDateTime().Nullable()
            .WithColumn("thumbnail_path").AsString(2000).NotNullable().WithDefaultValue(string.Empty)
            .WithColumn("content_preview").AsString(int.MaxValue).NotNullable().WithDefaultValue(string.Empty);

        Create.Index("IX_files_file_hash").OnTable("files").OnColumn("file_hash");
        Create.Index("IX_files_file_extension").OnTable("files").OnColumn("file_extension");
        Create.Index("IX_files_status").OnTable("files").OnColumn("status");
        Create.Index("IX_files_imported_at").OnTable("files").OnColumn("imported_at");
    }

    public override void Down()
    {
        Delete.Table("files");
    }
}
