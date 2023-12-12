using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class PlaylistMediaOptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Options",
                table: "Playlists",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Options",
                table: "Medias",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Options",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "Options",
                table: "Medias");
        }
    }
}
