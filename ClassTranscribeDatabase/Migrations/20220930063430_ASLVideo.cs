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
                    Link = table.Column<string>(nullable: true),
                    Source = table.Column<string>(nullable: true),
                    LicenseTag = table.Column<string>(nullable: true),
                    Shared = table.Column<bool>(nullable: false),
                    Editable = table.Column<bool>(nullable: false),
                    Domain = table.Column<string>(nullable: true),
                    Likes = table.Column<int>(nullable: false),
                    CourseId = table.Column<string>(nullable: true),
                    OfferingId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASLVideos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ASLVideos_CourseOfferings_CourseId_OfferingId",
                        columns: x => new { x.CourseId, x.OfferingId },
                        principalTable: "CourseOfferings",
                        principalColumns: new[] { "CourseId", "OfferingId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ASLVideos_CourseId_OfferingId",
                table: "ASLVideos",
                columns: new[] { "CourseId", "OfferingId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
        }
    }
}