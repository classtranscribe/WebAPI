using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class TranscriptionsUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Editable",
                table: "Transcriptions",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "Transcriptions",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PublishStatus",
                table: "Transcriptions",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SourceInternalRef",
                table: "Transcriptions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceLabel",
                table: "Transcriptions",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TranscriptionType",
                table: "Transcriptions",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Editable",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "PublishStatus",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "SourceInternalRef",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "SourceLabel",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "TranscriptionType",
                table: "Transcriptions");
        }
    }
}
