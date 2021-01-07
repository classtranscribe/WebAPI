using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class PublishStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Offerings");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Medias");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "EPubs");

            migrationBuilder.AddColumn<int>(
                name: "PublishStatus",
                table: "Playlists",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PublishStatus",
                table: "Offerings",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PublishStatus",
                table: "Medias",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PublishStatus",
                table: "EPubs",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                table: "EPubs",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishStatus",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "PublishStatus",
                table: "Offerings");

            migrationBuilder.DropColumn(
                name: "PublishStatus",
                table: "Medias");

            migrationBuilder.DropColumn(
                name: "PublishStatus",
                table: "EPubs");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "EPubs");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Playlists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Offerings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Medias",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "EPubs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
