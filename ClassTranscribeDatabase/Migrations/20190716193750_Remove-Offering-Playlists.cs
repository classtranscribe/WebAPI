using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class RemoveOfferingPlaylists : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfferingPlaylists");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "UserOfferings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Terms");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Offerings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Medias");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CourseOfferings");

            migrationBuilder.AddColumn<int>(
                name: "IsDeletedStatus",
                table: "Videos",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IsDeletedStatus",
                table: "UserOfferings",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IsDeletedStatus",
                table: "Universities",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IsDeletedStatus",
                table: "Transcriptions",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IsDeletedStatus",
                table: "Terms",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IsDeletedStatus",
                table: "Playlists",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Playlists",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfferingId",
                table: "Playlists",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsDeletedStatus",
                table: "Offerings",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IsDeletedStatus",
                table: "Medias",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IsDeletedStatus",
                table: "Departments",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IsDeletedStatus",
                table: "Courses",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IsDeletedStatus",
                table: "CourseOfferings",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_OfferingId",
                table: "Playlists",
                column: "OfferingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Playlists_Offerings_OfferingId",
                table: "Playlists",
                column: "OfferingId",
                principalTable: "Offerings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Playlists_Offerings_OfferingId",
                table: "Playlists");

            migrationBuilder.DropIndex(
                name: "IX_Playlists_OfferingId",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "IsDeletedStatus",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "IsDeletedStatus",
                table: "UserOfferings");

            migrationBuilder.DropColumn(
                name: "IsDeletedStatus",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "IsDeletedStatus",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "IsDeletedStatus",
                table: "Terms");

            migrationBuilder.DropColumn(
                name: "IsDeletedStatus",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "OfferingId",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "IsDeletedStatus",
                table: "Offerings");

            migrationBuilder.DropColumn(
                name: "IsDeletedStatus",
                table: "Medias");

            migrationBuilder.DropColumn(
                name: "IsDeletedStatus",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "IsDeletedStatus",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "IsDeletedStatus",
                table: "CourseOfferings");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Videos",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "UserOfferings",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Universities",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Transcriptions",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Terms",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Playlists",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Offerings",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Medias",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Departments",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Courses",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "CourseOfferings",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "OfferingPlaylists",
                columns: table => new
                {
                    OfferingId = table.Column<string>(nullable: false),
                    PlaylistId = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    Id = table.Column<string>(nullable: true),
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
                        name: "FK_OfferingPlaylists_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfferingPlaylists_PlaylistId",
                table: "OfferingPlaylists",
                column: "PlaylistId");
        }
    }
}
