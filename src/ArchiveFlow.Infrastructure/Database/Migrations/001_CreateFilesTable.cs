using FluentMigrator;

namespace ArchiveFlow.Infrastructure.Database.Migrations;

[Migration(202606180001, "Create the core files table for archive records")]
public class _001_CreateFilesTable : Migration
{
    public override void Up()
    {
        Create.Table("files")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("archive_id").AsString(50).NotNullable().Unique()
            .WithColumn("file_path").AsString(1000).NotNullable()
            .WithColumn("file_name").AsString(255).NotNullable()
            .WithColumn("file_extension").AsString(20).NotNullable()
            .WithColumn("file_hash").AsString(64).NotNullable()
            .WithColumn("file_size").AsInt64().NotNullable()
            .WithColumn("mime_type").AsString(100).NotNullable()
            .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("imported_at").AsDateTime().NotNullable()
            .WithColumn("modified_at").AsDateTime().Nullable()
            .WithColumn("last_scanned_at").AsDateTime().Nullable();

        Create.Index("IX_files_file_hash").OnTable("files").OnColumn("file_hash");
        Create.Index("IX_files_file_extension").OnTable("files").OnColumn("file_extension");
        Create.Index("IX_files_status").OnTable("files").OnColumn("status");
    }

    public override void Down()
    {
        Delete.Table("files");
    }
}
