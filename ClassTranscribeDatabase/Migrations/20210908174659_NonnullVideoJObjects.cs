using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class NonnullVideoJObjects : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Videos\" SET \"SceneData\" = '{}' WHERE \"SceneData\" IS NULL");
            migrationBuilder.Sql("UPDATE \"Videos\" SET \"JsonMetadata\" = '{}' WHERE \"JsonMetadata\" IS NULL");
            migrationBuilder.Sql("UPDATE \"Videos\" SET \"FileMediaInfo\" = '{}' WHERE \"FileMediaInfo\" IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "SceneData",
                table: "Videos",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "JsonMetadata",
                table: "Videos",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileMediaInfo",
                table: "Videos",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SceneData",
                table: "Videos",
                type: "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "JsonMetadata",
                table: "Videos",
                type: "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "FileMediaInfo",
                table: "Videos",
                type: "text",
                nullable: true,
                oldClrType: typeof(string));
        }
    }
}
