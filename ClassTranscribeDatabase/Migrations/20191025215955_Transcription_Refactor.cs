using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Transcription_Refactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoId",
                table: "Transcriptions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoId",
                table: "Transcriptions");
        }
    }
}
