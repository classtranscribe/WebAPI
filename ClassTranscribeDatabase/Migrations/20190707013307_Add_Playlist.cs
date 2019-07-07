using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Add_Playlist : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Medias_Playlist_PlaylistId",
                table: "Medias");

            migrationBuilder.DropForeignKey(
                name: "FK_OfferingPlaylists_Playlist_PlaylistId",
                table: "OfferingPlaylists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Playlist",
                table: "Playlist");

            migrationBuilder.RenameTable(
                name: "Playlist",
                newName: "Playlists");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Playlists",
                table: "Playlists",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Medias_Playlists_PlaylistId",
                table: "Medias",
                column: "PlaylistId",
                principalTable: "Playlists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfferingPlaylists_Playlists_PlaylistId",
                table: "OfferingPlaylists",
                column: "PlaylistId",
                principalTable: "Playlists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Medias_Playlists_PlaylistId",
                table: "Medias");

            migrationBuilder.DropForeignKey(
                name: "FK_OfferingPlaylists_Playlists_PlaylistId",
                table: "OfferingPlaylists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Playlists",
                table: "Playlists");

            migrationBuilder.RenameTable(
                name: "Playlists",
                newName: "Playlist");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Playlist",
                table: "Playlist",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Medias_Playlist_PlaylistId",
                table: "Medias",
                column: "PlaylistId",
                principalTable: "Playlist",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfferingPlaylists_Playlist_PlaylistId",
                table: "OfferingPlaylists",
                column: "PlaylistId",
                principalTable: "Playlist",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
