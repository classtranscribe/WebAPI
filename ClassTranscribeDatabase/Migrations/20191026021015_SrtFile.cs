using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class SrtFile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SrtFileId",
                table: "Transcriptions",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transcriptions_SrtFileId",
                table: "Transcriptions",
                column: "SrtFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transcriptions_FileRecords_SrtFileId",
                table: "Transcriptions",
                column: "SrtFileId",
                principalTable: "FileRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transcriptions_FileRecords_SrtFileId",
                table: "Transcriptions");

            migrationBuilder.DropIndex(
                name: "IX_Transcriptions_SrtFileId",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "SrtFileId",
                table: "Transcriptions");
        }
    }
}
