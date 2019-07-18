using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class FileRecordsForeignKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Video1Id",
                table: "Videos",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Video2Id",
                table: "Videos",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Videos_Video1Id",
                table: "Videos",
                column: "Video1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_Video2Id",
                table: "Videos",
                column: "Video2Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_FileRecords_Video1Id",
                table: "Videos",
                column: "Video1Id",
                principalTable: "FileRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_FileRecords_Video2Id",
                table: "Videos",
                column: "Video2Id",
                principalTable: "FileRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Videos_FileRecords_Video1Id",
                table: "Videos");

            migrationBuilder.DropForeignKey(
                name: "FK_Videos_FileRecords_Video2Id",
                table: "Videos");

            migrationBuilder.DropIndex(
                name: "IX_Videos_Video1Id",
                table: "Videos");

            migrationBuilder.DropIndex(
                name: "IX_Videos_Video2Id",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "Video1Id",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "Video2Id",
                table: "Videos");
        }
    }
}
