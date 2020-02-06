using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Caption_Table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Transcriptions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Transcriptions",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Caption",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(nullable: false),
                    LastUpdatedBy = table.Column<string>(nullable: true),
                    IsDeletedStatus = table.Column<int>(nullable: false),
                    Begin = table.Column<TimeSpan>(nullable: false),
                    End = table.Column<TimeSpan>(nullable: false),
                    Text = table.Column<string>(nullable: true),
                    TranscriptionId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Caption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Caption_Transcriptions_TranscriptionId",
                        column: x => x.TranscriptionId,
                        principalTable: "Transcriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Caption_TranscriptionId",
                table: "Caption",
                column: "TranscriptionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Caption");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Transcriptions");
        }
    }
}
