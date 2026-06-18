using FluentMigrator;

namespace ArchiveFlow.Infrastructure.Database.Migrations;

[Migration(202606180002, "Create EAV Metadata tables with categories")]
public class _002_CreateMetadataTables : Migration
{
    public override void Up()
    {
        Create.Table("metadata_fields")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("field_name").AsString(100).NotNullable().Unique()
            .WithColumn("display_name").AsString(100).NotNullable()
            .WithColumn("field_type").AsString(50).NotNullable().WithDefaultValue("String")
            .WithColumn("category").AsString(50).NotNullable().WithDefaultValue("Basic")
            .WithColumn("is_required").AsBoolean().NotNullable().WithDefaultValue(false);

        Create.Table("metadata_values")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("file_id").AsString(36).NotNullable()
            .WithColumn("field_id").AsInt32().NotNullable()
            .WithColumn("value_text").AsString(1000).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        Create.Index("IX_metadata_values_file_id").OnTable("metadata_values").OnColumn("file_id");
        Create.Index("IX_metadata_values_field_id").OnTable("metadata_values").OnColumn("field_id");
    }

    public override void Down()
    {
        Delete.Table("metadata_values");
        Delete.Table("metadata_fields");
    }
}