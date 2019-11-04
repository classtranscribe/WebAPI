using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class DropCourseNameDescFromCourses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseName",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Courses");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CourseName",
                table: "Courses",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Courses",
                nullable: true);
        }
    }
}
