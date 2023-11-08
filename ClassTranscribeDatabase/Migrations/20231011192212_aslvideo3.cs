using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class aslvideo3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ASLVideoId",
                table: "Videos",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedASLVideoId",
                table: "Videos",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingLog",
                table: "Videos",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Videos_ASLVideoId",
                table: "Videos",
                column: "ASLVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_ProcessedASLVideoId",
                table: "Videos",
                column: "ProcessedASLVideoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_FileRecords_ASLVideoId",
                table: "Videos",
                column: "ASLVideoId",
                principalTable: "FileRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_FileRecords_ProcessedASLVideoId",
                table: "Videos",
                column: "ProcessedASLVideoId",
                principalTable: "FileRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Videos_FileRecords_ASLVideoId",
                table: "Videos");

            migrationBuilder.DropForeignKey(
                name: "FK_Videos_FileRecords_ProcessedASLVideoId",
                table: "Videos");

            migrationBuilder.DropIndex(
                name: "IX_Videos_ASLVideoId",
                table: "Videos");

            migrationBuilder.DropIndex(
                name: "IX_Videos_ProcessedASLVideoId",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "ASLVideoId",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "ProcessedASLVideoId",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "ProcessingLog",
                table: "Videos");
        }
    }
}
