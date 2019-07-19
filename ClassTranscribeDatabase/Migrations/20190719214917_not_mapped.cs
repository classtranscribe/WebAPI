using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class not_mapped : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Path",
                table: "FileRecords",
                newName: "PrivatePath");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "FileRecords",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "FileRecords");

            migrationBuilder.RenameColumn(
                name: "PrivatePath",
                table: "FileRecords",
                newName: "Path");
        }
    }
}
