using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class FilePath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Courses",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "CourseOfferings",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "CourseOfferings");
        }
    }
}
