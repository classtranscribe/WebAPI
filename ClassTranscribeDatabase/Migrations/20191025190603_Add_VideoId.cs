using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Add_VideoId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoId",
                table: "Medias",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoId",
                table: "Medias");
        }
    }
}
