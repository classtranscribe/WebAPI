using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Media_modification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaSource",
                table: "Medias");

            migrationBuilder.RenameColumn(
                name: "MediaUrl",
                table: "Medias",
                newName: "UniqueMediaIdentifier");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UniqueMediaIdentifier",
                table: "Medias",
                newName: "MediaUrl");

            migrationBuilder.AddColumn<string>(
                name: "MediaSource",
                table: "Medias",
                nullable: true);
        }
    }
}
