using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class TaskLogString : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessingLog",
                table: "Videos");

            migrationBuilder.AddColumn<string>(
                name: "TaskLog",
                table: "Videos",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaskLog",
                table: "Videos");

            migrationBuilder.AddColumn<string>(
                name: "ProcessingLog",
                table: "Videos",
                type: "text",
                nullable: true);
        }
    }
}
