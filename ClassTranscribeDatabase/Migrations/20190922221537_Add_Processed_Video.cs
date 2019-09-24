using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Add_Processed_Video : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProcessedVideo1Id",
                table: "Videos",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedVideo2Id",
                table: "Videos",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Videos_ProcessedVideo1Id",
                table: "Videos",
                column: "ProcessedVideo1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_ProcessedVideo2Id",
                table: "Videos",
                column: "ProcessedVideo2Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_FileRecords_ProcessedVideo1Id",
                table: "Videos",
                column: "ProcessedVideo1Id",
                principalTable: "FileRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_FileRecords_ProcessedVideo2Id",
                table: "Videos",
                column: "ProcessedVideo2Id",
                principalTable: "FileRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Videos_FileRecords_ProcessedVideo1Id",
                table: "Videos");

            migrationBuilder.DropForeignKey(
                name: "FK_Videos_FileRecords_ProcessedVideo2Id",
                table: "Videos");

            migrationBuilder.DropIndex(
                name: "IX_Videos_ProcessedVideo1Id",
                table: "Videos");

            migrationBuilder.DropIndex(
                name: "IX_Videos_ProcessedVideo2Id",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "ProcessedVideo1Id",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "ProcessedVideo2Id",
                table: "Videos");
        }
    }
}
