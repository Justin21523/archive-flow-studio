using FluentMigrator;

namespace ArchiveFlow.Infrastructure.Database.Migrations;

/// <summary>
/// Creates dynamic metadata tables used by the Metadata Editor and action nodes.
/// </summary>
[Migration(202606210002, "Create metadata field and value tables")]
public sealed class _002_CreateMetadataTables : Migration
{
    public override void Up()
    {
        if (!Schema.Table("metadata_fields").Exists())
        {
            Create.Table("metadata_fields")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("field_name").AsString(128).NotNullable().Unique()
                .WithColumn("display_name").AsString(256).NotNullable()
                .WithColumn("field_type").AsString(64).NotNullable().WithDefaultValue("String")
                .WithColumn("category").AsString(128).NotNullable().WithDefaultValue("Basic")
                .WithColumn("is_required").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("sort_order").AsInt32().NotNullable().WithDefaultValue(0);

            Create.Index("IX_metadata_fields_field_name")
                .OnTable("metadata_fields")
                .OnColumn("field_name");
        }
        else
        {
            if (!Schema.Table("metadata_fields").Column("category").Exists())
            {
                Alter.Table("metadata_fields")
                    .AddColumn("category")
                    .AsString(128)
                    .NotNullable()
                    .WithDefaultValue("Basic");
            }

            if (!Schema.Table("metadata_fields").Column("is_required").Exists())
            {
                Alter.Table("metadata_fields")
                    .AddColumn("is_required")
                    .AsBoolean()
                    .NotNullable()
                    .WithDefaultValue(false);
            }

            if (!Schema.Table("metadata_fields").Column("sort_order").Exists())
            {
                Alter.Table("metadata_fields")
                    .AddColumn("sort_order")
                    .AsInt32()
                    .NotNullable()
                    .WithDefaultValue(0);
            }
        }

        if (!Schema.Table("metadata_values").Exists())
        {
            Create.Table("metadata_values")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("file_id").AsString(64).NotNullable()
                .WithColumn("field_id").AsInt32().NotNullable()
                .WithColumn("value_text").AsString(int.MaxValue).Nullable()
                .WithColumn("created_at").AsDateTime().NotNullable()
                .WithColumn("updated_at").AsDateTime().Nullable();

            Create.Index("IX_metadata_values_file_id")
                .OnTable("metadata_values")
                .OnColumn("file_id");

            Create.Index("IX_metadata_values_field_id")
                .OnTable("metadata_values")
                .OnColumn("field_id");
        }
        else
        {
            if (!Schema.Table("metadata_values").Column("updated_at").Exists())
            {
                Alter.Table("metadata_values")
                    .AddColumn("updated_at")
                    .AsDateTime()
                    .Nullable();
            }
        }
    }

    public override void Down()
    {
        if (Schema.Table("metadata_values").Exists())
        {
            Delete.Table("metadata_values");
        }

        if (Schema.Table("metadata_fields").Exists())
        {
            Delete.Table("metadata_fields");
        }
    }
}