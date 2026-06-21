using FluentMigrator;

namespace ArchiveFlow.Infrastructure.Database.Migrations;

/// <summary>
/// Ensures metadata tables contain all columns required by the standalone metadata editor.
/// </summary>
[Migration(202606210003, "Ensure metadata editor schema")]
public sealed class _003_EnsureMetadataEditorSchema : Migration
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
        }
        else
        {
            if (!Schema.Table("metadata_fields").Column("field_type").Exists())
            {
                Alter.Table("metadata_fields")
                    .AddColumn("field_type")
                    .AsString(64)
                    .NotNullable()
                    .WithDefaultValue("String");
            }

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

        if (!Schema.Table("metadata_values").Index("IX_metadata_values_file_id").Exists())
        {
            Create.Index("IX_metadata_values_file_id")
                .OnTable("metadata_values")
                .OnColumn("file_id");
        }

        if (!Schema.Table("metadata_values").Index("IX_metadata_values_field_id").Exists())
        {
            Create.Index("IX_metadata_values_field_id")
                .OnTable("metadata_values")
                .OnColumn("field_id");
        }
    }

    public override void Down()
    {
        // This migration is intentionally non-destructive.
    }
}