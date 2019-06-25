using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Status : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Videos",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "UserOfferings",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Universities",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Transcriptions",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Terms",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UniversityId",
                table: "Terms",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Offerings",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "OfferingMedias",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Medias",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Departments",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Courses",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "CourseOfferings",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Terms_UniversityId",
                table: "Terms",
                column: "UniversityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Terms_Universities_UniversityId",
                table: "Terms",
                column: "UniversityId",
                principalTable: "Universities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Terms_Universities_UniversityId",
                table: "Terms");

            migrationBuilder.DropIndex(
                name: "IX_Terms_UniversityId",
                table: "Terms");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "UserOfferings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Terms");

            migrationBuilder.DropColumn(
                name: "UniversityId",
                table: "Terms");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Offerings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OfferingMedias");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Medias");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CourseOfferings");
        }
    }
}
