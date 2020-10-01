using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class EpubApis : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EPubs_FileRecords_FileId",
                table: "EPubs");

            migrationBuilder.DropTable(
                name: "EPubChapters");

            migrationBuilder.DropIndex(
                name: "IX_EPubs_FileId",
                table: "EPubs");

            migrationBuilder.DropColumn(
                name: "FileId",
                table: "EPubs");

            migrationBuilder.DropColumn(
                name: "Json",
                table: "EPubs");

            migrationBuilder.AddColumn<string>(
                name: "Author",
                table: "EPubs",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Chapters",
                table: "EPubs",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cover",
                table: "EPubs",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Filename",
                table: "EPubs",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "EPubs",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Publisher",
                table: "EPubs",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceId",
                table: "EPubs",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "EPubs",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "EPubs",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(nullable: false),
                    LastUpdatedBy = table.Column<string>(nullable: true),
                    IsDeletedStatus = table.Column<int>(nullable: false),
                    SourceType = table.Column<int>(nullable: false),
                    SourceId = table.Column<string>(nullable: true),
                    ImageFileId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Images_FileRecords_ImageFileId",
                        column: x => x.ImageFileId,
                        principalTable: "FileRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Images_ImageFileId",
                table: "Images",
                column: "ImageFileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropColumn(
                name: "Author",
                table: "EPubs");

            migrationBuilder.DropColumn(
                name: "Chapters",
                table: "EPubs");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "EPubs");

            migrationBuilder.DropColumn(
                name: "Filename",
                table: "EPubs");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "EPubs");

            migrationBuilder.DropColumn(
                name: "Publisher",
                table: "EPubs");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "EPubs");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "EPubs");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "EPubs");

            migrationBuilder.AddColumn<string>(
                name: "FileId",
                table: "EPubs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Json",
                table: "EPubs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EPubChapters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Data = table.Column<string>(type: "text", nullable: true),
                    EPubId = table.Column<string>(type: "text", nullable: true),
                    IsDeletedStatus = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EPubChapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EPubChapters_EPubs_EPubId",
                        column: x => x.EPubId,
                        principalTable: "EPubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EPubs_FileId",
                table: "EPubs",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_EPubChapters_EPubId",
                table: "EPubChapters",
                column: "EPubId");

            migrationBuilder.AddForeignKey(
                name: "FK_EPubs_FileRecords_FileId",
                table: "EPubs",
                column: "FileId",
                principalTable: "FileRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
