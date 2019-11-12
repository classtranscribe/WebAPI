using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class EpubTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EPub_FileRecords_FileId",
                table: "EPub");

            migrationBuilder.DropForeignKey(
                name: "FK_EPub_Videos_VideoId",
                table: "EPub");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EPub",
                table: "EPub");

            migrationBuilder.RenameTable(
                name: "EPub",
                newName: "EPubs");

            migrationBuilder.RenameIndex(
                name: "IX_EPub_VideoId",
                table: "EPubs",
                newName: "IX_EPubs_VideoId");

            migrationBuilder.RenameIndex(
                name: "IX_EPub_FileId",
                table: "EPubs",
                newName: "IX_EPubs_FileId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EPubs",
                table: "EPubs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EPubs_FileRecords_FileId",
                table: "EPubs",
                column: "FileId",
                principalTable: "FileRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EPubs_Videos_VideoId",
                table: "EPubs",
                column: "VideoId",
                principalTable: "Videos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EPubs_FileRecords_FileId",
                table: "EPubs");

            migrationBuilder.DropForeignKey(
                name: "FK_EPubs_Videos_VideoId",
                table: "EPubs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EPubs",
                table: "EPubs");

            migrationBuilder.RenameTable(
                name: "EPubs",
                newName: "EPub");

            migrationBuilder.RenameIndex(
                name: "IX_EPubs_VideoId",
                table: "EPub",
                newName: "IX_EPub_VideoId");

            migrationBuilder.RenameIndex(
                name: "IX_EPubs_FileId",
                table: "EPub",
                newName: "IX_EPub_FileId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EPub",
                table: "EPub",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EPub_FileRecords_FileId",
                table: "EPub",
                column: "FileId",
                principalTable: "FileRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EPub_Videos_VideoId",
                table: "EPub",
                column: "VideoId",
                principalTable: "Videos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
