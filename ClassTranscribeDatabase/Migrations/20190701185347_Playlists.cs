using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Playlists : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfferingMedias");

            migrationBuilder.AddColumn<string>(
                name: "PlaylistId",
                table: "Medias",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Playlist",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(nullable: false),
                    LastUpdatedBy = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    SourceType = table.Column<int>(nullable: false),
                    PlaylistIdentifier = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playlist", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OfferingPlaylists",
                columns: table => new
                {
                    OfferingId = table.Column<string>(nullable: false),
                    PlaylistId = table.Column<string>(nullable: false),
                    Id = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(nullable: false),
                    LastUpdatedBy = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferingPlaylists", x => new { x.OfferingId, x.PlaylistId });
                    table.ForeignKey(
                        name: "FK_OfferingPlaylists_Offerings_OfferingId",
                        column: x => x.OfferingId,
                        principalTable: "Offerings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OfferingPlaylists_Playlist_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "Playlist",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Medias_PlaylistId",
                table: "Medias",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferingPlaylists_PlaylistId",
                table: "OfferingPlaylists",
                column: "PlaylistId");

            migrationBuilder.AddForeignKey(
                name: "FK_Medias_Playlist_PlaylistId",
                table: "Medias",
                column: "PlaylistId",
                principalTable: "Playlist",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Medias_Playlist_PlaylistId",
                table: "Medias");

            migrationBuilder.DropTable(
                name: "OfferingPlaylists");

            migrationBuilder.DropTable(
                name: "Playlist");

            migrationBuilder.DropIndex(
                name: "IX_Medias_PlaylistId",
                table: "Medias");

            migrationBuilder.DropColumn(
                name: "PlaylistId",
                table: "Medias");

            migrationBuilder.CreateTable(
                name: "OfferingMedias",
                columns: table => new
                {
                    OfferingId = table.Column<string>(nullable: false),
                    MediaId = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    Id = table.Column<string>(nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(nullable: false),
                    LastUpdatedBy = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferingMedias", x => new { x.OfferingId, x.MediaId });
                    table.ForeignKey(
                        name: "FK_OfferingMedias_Medias_MediaId",
                        column: x => x.MediaId,
                        principalTable: "Medias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OfferingMedias_Offerings_OfferingId",
                        column: x => x.OfferingId,
                        principalTable: "Offerings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfferingMedias_MediaId",
                table: "OfferingMedias",
                column: "MediaId");
        }
    }
}
