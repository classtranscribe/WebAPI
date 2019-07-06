using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class MediaJon : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Path",
                table: "Videos",
                newName: "Video2Path");

            migrationBuilder.AddColumn<string>(
                name: "Video1Path",
                table: "Videos",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "Medias",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Video1Path",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "Medias");

            migrationBuilder.RenameColumn(
                name: "Video2Path",
                table: "Videos",
                newName: "Path");
        }
    }
}
