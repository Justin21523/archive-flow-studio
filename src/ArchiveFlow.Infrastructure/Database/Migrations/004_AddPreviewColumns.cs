using FluentMigrator;

namespace ArchiveFlow.Infrastructure.Database.Migrations;

[Migration(202606180004, "Add thumbnail and content preview columns to files table")]
public class _004_AddPreviewColumns : Migration
{
    public override void Up()
    {
        Alter.Table("files")
            .AddColumn("thumbnail_path").AsString(500).Nullable()
            .AddColumn("content_preview").AsString(2000).Nullable();
    }

    public override void Down()
    {
        Delete.Column("thumbnail_path").FromTable("files");
        Delete.Column("content_preview").FromTable("files");
    }
}