using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Transcription_Refactorv2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transcriptions_Medias_MediaId",
                table: "Transcriptions");

            migrationBuilder.DropIndex(
                name: "IX_Transcriptions_MediaId",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "MediaId",
                table: "Transcriptions");

            migrationBuilder.CreateIndex(
                name: "IX_Transcriptions_VideoId",
                table: "Transcriptions",
                column: "VideoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transcriptions_Videos_VideoId",
                table: "Transcriptions",
                column: "VideoId",
                principalTable: "Videos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transcriptions_Videos_VideoId",
                table: "Transcriptions");

            migrationBuilder.DropIndex(
                name: "IX_Transcriptions_VideoId",
                table: "Transcriptions");

            migrationBuilder.AddColumn<string>(
                name: "MediaId",
                table: "Transcriptions",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transcriptions_MediaId",
                table: "Transcriptions",
                column: "MediaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transcriptions_Medias_MediaId",
                table: "Transcriptions",
                column: "MediaId",
                principalTable: "Medias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
