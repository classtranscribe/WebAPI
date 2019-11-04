using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class CourseNameDesc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CourseName",
                table: "Offerings",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Offerings",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseName",
                table: "Offerings");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Offerings");
        }
    }
}
