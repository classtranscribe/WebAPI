using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class PhraseHintJsonTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropColumn(
            //     name: "ASLVideo",
            //     table: "Videos");

            migrationBuilder.AddColumn<string>(
                name: "PhraseHintDataId",
                table: "Videos",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JsonData",
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
                    Json = table.Column<string>(nullable: true)
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Videos_JsonData_PhraseHintDataId",
                table: "Videos");

            migrationBuilder.DropTable(
                name: "JsonData");

            // migrationBuilder.DropIndex(
            //     name: "IX_Videos_PhraseHintDataId",
            //     table: "Videos");

            migrationBuilder.DropColumn(
                name: "PhraseHintDataId",
                table: "Videos");

            // migrationBuilder.AddColumn<string>(
            //     name: "ASLVideo",
            //     table: "Videos",
            //     type: "text",
            //     nullable: false,
            //     defaultValue: "");
        }
    }
}
