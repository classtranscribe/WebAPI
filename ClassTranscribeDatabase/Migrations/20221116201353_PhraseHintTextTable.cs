using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class PhraseHintTextTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Videos_JsonData_PhraseHintDataId",
                table: "Videos");

            migrationBuilder.DropTable(
                name: "JsonData");

            migrationBuilder.DropIndex(
                name: "IX_Videos_PhraseHintDataId",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "PhraseHintDataId",
                table: "Videos");

            migrationBuilder.AddColumn<string>(
                name: "PhraseHintsDataId",
                table: "Videos",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TextData",
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
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Videos_PhraseHintsDataId",
                table: "Videos",
                column: "PhraseHintsDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_TextData_PhraseHintsDataId",
                table: "Videos",
                column: "PhraseHintsDataId",
                principalTable: "TextData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Videos_TextData_PhraseHintsDataId",
                table: "Videos");

            migrationBuilder.DropTable(
                name: "TextData");

            migrationBuilder.DropIndex(
                name: "IX_Videos_PhraseHintsDataId",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "PhraseHintsDataId",
                table: "Videos");

            migrationBuilder.AddColumn<string>(
                name: "PhraseHintDataId",
                table: "Videos",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JsonData",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeletedStatus = table.Column<int>(type: "integer", nullable: false),
                    Json = table.Column<string>(type: "text", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JsonData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Videos_PhraseHintDataId",
                table: "Videos",
                column: "PhraseHintDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_JsonData_PhraseHintDataId",
                table: "Videos",
                column: "PhraseHintDataId",
                principalTable: "JsonData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
