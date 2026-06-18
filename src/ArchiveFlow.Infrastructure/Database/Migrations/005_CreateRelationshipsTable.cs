using FluentMigrator;

namespace ArchiveFlow.Infrastructure.Database.Migrations;

/// <summary>
/// Migration to create the table for storing relationships between files.
/// </summary>
[Migration(202606180005, "Create relationships table for knowledge graph")]
public class _005_CreateRelationshipsTable : Migration
{
    public override void Up()
    {
        Create.Table("file_relationships")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("source_file_id").AsString(36).NotNullable()
            .WithColumn("target_file_id").AsString(36).NotNullable()
            .WithColumn("relationship_type").AsString(50).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        Create.Index("IX_relationships_source").OnTable("file_relationships").OnColumn("source_file_id");
        Create.Index("IX_relationships_target").OnTable("file_relationships").OnColumn("target_file_id");
    }

    public override void Down()
    {
        Delete.Table("file_relationships");
    }
}