using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Caption_Table2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Caption_Transcriptions_TranscriptionId",
                table: "Caption");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Caption",
                table: "Caption");

            migrationBuilder.RenameTable(
                name: "Caption",
                newName: "Captions");

            migrationBuilder.RenameIndex(
                name: "IX_Caption_TranscriptionId",
                table: "Captions",
                newName: "IX_Captions_TranscriptionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Captions",
                table: "Captions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Captions_Transcriptions_TranscriptionId",
                table: "Captions",
                column: "TranscriptionId",
                principalTable: "Transcriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Captions_Transcriptions_TranscriptionId",
                table: "Captions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Captions",
                table: "Captions");

            migrationBuilder.RenameTable(
                name: "Captions",
                newName: "Caption");

            migrationBuilder.RenameIndex(
                name: "IX_Captions_TranscriptionId",
                table: "Caption",
                newName: "IX_Caption_TranscriptionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Caption",
                table: "Caption",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Caption_Transcriptions_TranscriptionId",
                table: "Caption",
                column: "TranscriptionId",
                principalTable: "Transcriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
