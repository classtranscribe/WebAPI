using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassTranscribeDatabase.Migrations
{
    /// <inheritdoc />
    public partial class VideoFlashDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FlashDetectionDataId",
                table: "Videos",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlashDetectionDataId",
                table: "Videos");
        }
    }
}
