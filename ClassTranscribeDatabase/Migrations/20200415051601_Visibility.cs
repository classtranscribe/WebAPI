using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Visibility : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                table: "Playlists",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                table: "Offerings",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                table: "Medias",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(nullable: false),
                    LastUpdatedBy = table.Column<string>(nullable: true),
                    IsDeletedStatus = table.Column<int>(nullable: false),
                    ApplicationUserId = table.Column<string>(nullable: true),
                    Payload = table.Column<string>(nullable: true),
                    LogLevel = table.Column<int>(nullable: false),
                    Ack = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(nullable: false),
                    LastUpdatedBy = table.Column<string>(nullable: true),
                    IsDeletedStatus = table.Column<int>(nullable: false),
                    ResourceType = table.Column<int>(nullable: false),
                    ResourceId = table.Column<string>(nullable: false),
                    ApplicationUserId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.UniqueConstraint("AK_Subscriptions_ResourceType_ResourceId_ApplicationUserId", x => new { x.ResourceType, x.ResourceId, x.ApplicationUserId });
                    table.ForeignKey(
                        name: "FK_Subscriptions_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ApplicationUserId",
                table: "Messages",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ApplicationUserId",
                table: "Subscriptions",
                column: "ApplicationUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Offerings");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Medias");
        }
    }
}
