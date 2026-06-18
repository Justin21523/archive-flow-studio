using FluentMigrator;

namespace ArchiveFlow.Infrastructure.Database.Migrations;

/// <summary>
/// Migration to create the table for storing Smart Collection definitions.
/// </summary>
[Migration(202606180007, "Create smart_collections table for dynamic file grouping")]
public class _007_CreateSmartCollectionsTable : Migration
{
    public override void Up()
    {
        if (Schema.Table("smart_collections").Exists())
        {
            return;
        }

        Create.Table("smart_collections")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("name").AsString(255).NotNullable()
            .WithColumn("filter_rule_json").AsString(2000).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable();
    }

    public override void Down()
    {
        if (Schema.Table("smart_collections").Exists())
        {
            Delete.Table("smart_collections");
        }
    }
}
