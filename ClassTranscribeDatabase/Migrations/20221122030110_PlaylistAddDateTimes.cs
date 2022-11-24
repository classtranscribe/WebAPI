using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class PlaylistAddDateTimes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ListCheckedAt",
                table: "Playlists",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ListUpdatedAt",
                table: "Playlists",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ListCheckedAt",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "ListUpdatedAt",
                table: "Playlists");
        }
    }
}
