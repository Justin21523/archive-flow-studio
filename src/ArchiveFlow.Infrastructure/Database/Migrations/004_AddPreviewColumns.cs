using FluentMigrator;

namespace ArchiveFlow.Infrastructure.Database.Migrations;

[Migration(202606180004, "Add thumbnail and content preview columns to files table")]
public class _004_AddPreviewColumns : Migration
{
    public override void Up()
    {
        if (!Schema.Table("files").Column("thumbnail_path").Exists())
        {
            Alter.Table("files")
                .AddColumn("thumbnail_path").AsString(500).Nullable();
        }

        if (!Schema.Table("files").Column("content_preview").Exists())
        {
            Alter.Table("files")
                .AddColumn("content_preview").AsString(2000).Nullable();
        }
    }

    public override void Down()
    {
        if (Schema.Table("files").Column("thumbnail_path").Exists())
        {
            Delete.Column("thumbnail_path").FromTable("files");
        }

        if (Schema.Table("files").Column("content_preview").Exists())
        {
            Delete.Column("content_preview").FromTable("files");
        }
    }
}
