using FluentMigrator;

namespace ArchiveFlow.Infrastructure.Database.Migrations;

[Migration(202606180002, "Create EAV Metadata tables")]
public class _002_CreateMetadataTables : Migration
{
    public override void Up()
    {
        // 定義 Metadata 欄位 (例如：Subject, Tag, Author)
        Create.Table("metadata_fields")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("field_name").AsString(100).NotNullable().Unique()
            .WithColumn("display_name").AsString(100).NotNullable()
            .WithColumn("field_type").AsString(50).NotNullable().WithDefaultValue("String");

        // 儲存實際的 Metadata 值 (Entity-Attribute-Value 模型)
        // 注意：移除了 FluentMigrator 的 ForeignKey 語法，因為 SQLite 不支援這種寫法
        Create.Table("metadata_values")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("file_id").AsString(36).NotNullable() // 對應 files.id (Guid as string)
            .WithColumn("field_id").AsInt32().NotNullable()
            .WithColumn("value_text").AsString(1000).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable();
            
        // 建立 Index 來加速 Metadata 查詢
        Create.Index("IX_metadata_values_file_id").OnTable("metadata_values").OnColumn("file_id");
        Create.Index("IX_metadata_values_field_id").OnTable("metadata_values").OnColumn("field_id");
    }

    public override void Down()
    {
        Delete.Table("metadata_values");
        Delete.Table("metadata_fields");
    }
}