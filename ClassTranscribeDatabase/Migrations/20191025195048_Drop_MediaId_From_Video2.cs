using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Drop_MediaId_From_Video2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Videos_Medias_MediaId",
                table: "Videos");

            migrationBuilder.DropIndex(
                name: "IX_Videos_MediaId",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "MediaId",
                table: "Videos");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MediaId",
                table: "Videos",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Videos_MediaId",
                table: "Videos",
                column: "MediaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_Medias_MediaId",
                table: "Videos",
                column: "MediaId",
                principalTable: "Medias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
