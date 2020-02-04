using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Add_Medias_to_Video : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Medias_VideoId",
                table: "Medias",
                column: "VideoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Medias_Videos_VideoId",
                table: "Medias",
                column: "VideoId",
                principalTable: "Videos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Medias_Videos_VideoId",
                table: "Medias");

            migrationBuilder.DropIndex(
                name: "IX_Medias_VideoId",
                table: "Medias");
        }
    }
}
