using FluentMigrator;

namespace ArchiveFlow.Infrastructure.Database.Migrations;

[Migration(202606180006, "Add category and required flags to metadata fields")]
public class _006_AddMetadataFieldAttributes : Migration
{
    public override void Up()
    {
        if (!Schema.Table("metadata_fields").Column("category").Exists())
        {
            Alter.Table("metadata_fields")
                .AddColumn("category").AsString(50).NotNullable().WithDefaultValue("Basic");
        }

        if (!Schema.Table("metadata_fields").Column("is_required").Exists())
        {
            Alter.Table("metadata_fields")
                .AddColumn("is_required").AsBoolean().NotNullable().WithDefaultValue(false);
        }
    }

    public override void Down()
    {
        if (Schema.Table("metadata_fields").Column("is_required").Exists())
        {
            Delete.Column("is_required").FromTable("metadata_fields");
        }

        if (Schema.Table("metadata_fields").Column("category").Exists())
        {
            Delete.Column("category").FromTable("metadata_fields");
        }
    }
}
