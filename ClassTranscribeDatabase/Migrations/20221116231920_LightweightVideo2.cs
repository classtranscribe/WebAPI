using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class LightweightVideo2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Videos_TextData_PhraseHintsDataId",
                table: "Videos");

            migrationBuilder.DropIndex(
                name: "IX_Videos_PhraseHintsDataId",
                table: "Videos");

            migrationBuilder.AddColumn<string>(
                name: "SceneObjectDataId",
                table: "Videos",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SceneObjectDataId",
                table: "Videos");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_PhraseHintsDataId",
                table: "Videos",
                column: "PhraseHintsDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_TextData_PhraseHintsDataId",
                table: "Videos",
                column: "PhraseHintsDataId",
                principalTable: "TextData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
