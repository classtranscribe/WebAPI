using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class FileRecord : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioPath",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "Video1Path",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "FileRecords");

            migrationBuilder.RenameColumn(
                name: "Video2Path",
                table: "Videos",
                newName: "AudioId");

            migrationBuilder.RenameColumn(
                name: "Path",
                table: "Transcriptions",
                newName: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_AudioId",
                table: "Videos",
                column: "AudioId");

            migrationBuilder.CreateIndex(
                name: "IX_Transcriptions_FileId",
                table: "Transcriptions",
                column: "FileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transcriptions_FileRecords_FileId",
                table: "Transcriptions",
                column: "FileId",
                principalTable: "FileRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_FileRecords_AudioId",
                table: "Videos",
                column: "AudioId",
                principalTable: "FileRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transcriptions_FileRecords_FileId",
                table: "Transcriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Videos_FileRecords_AudioId",
                table: "Videos");

            migrationBuilder.DropIndex(
                name: "IX_Videos_AudioId",
                table: "Videos");

            migrationBuilder.DropIndex(
                name: "IX_Transcriptions_FileId",
                table: "Transcriptions");

            migrationBuilder.RenameColumn(
                name: "AudioId",
                table: "Videos",
                newName: "Video2Path");

            migrationBuilder.RenameColumn(
                name: "FileId",
                table: "Transcriptions",
                newName: "Path");

            migrationBuilder.AddColumn<string>(
                name: "AudioPath",
                table: "Videos",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Video1Path",
                table: "Videos",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Transcriptions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "FileRecords",
                nullable: true);
        }
    }
}
