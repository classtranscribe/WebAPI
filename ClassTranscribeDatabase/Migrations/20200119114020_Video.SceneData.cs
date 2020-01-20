using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class VideoSceneData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JsonMetadata",
                table: "Videos",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SceneData",
                table: "Videos",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JsonMetadata",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "SceneData",
                table: "Videos");
        }
    }
}
