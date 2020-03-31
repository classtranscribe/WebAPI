using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Indexing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Index",
                table: "Playlists",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Index",
                table: "Medias",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Index",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "Index",
                table: "Medias");
        }
    }
}
