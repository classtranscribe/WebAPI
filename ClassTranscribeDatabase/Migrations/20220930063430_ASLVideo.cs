using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class ASLVideo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ASLVideos",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(nullable: false),
                    LastUpdatedBy = table.Column<string>(nullable: true),
                    IsDeletedStatus = table.Column<int>(nullable: false),
                    DeletedAt = table.Column<DateTime>(nullable: true),
                    DeletedBy = table.Column<string>(nullable: true),
                    Term = table.Column<string>(nullable: true),
                    Kind = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true),
                    WebsiteURL = table.Column<string>(nullable: true),
                    DownloadURL = table.Column<string>(nullable: true),
                    Source = table.Column<string>(nullable: true),
                    LicenseTag = table.Column<string>(nullable: true),
                    Domain = table.Column<string>(nullable: true),
                    Likes = table.Column<int>(nullable: false),
                    UUID = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASLVideos", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ASLVideos");
        }
    }
}
