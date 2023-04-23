using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class UpdateASLVideo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ASLVideos_CourseOfferings_CourseId_OfferingId",
                table: "ASLVideos");

            migrationBuilder.DropIndex(
                name: "IX_ASLVideos_CourseId_OfferingId",
                table: "ASLVideos");
            
            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "ASLVideos");

            migrationBuilder.DropColumn(
                name: "Editable",
                table: "ASLVideos");

            migrationBuilder.DropColumn(
                name: "Link",
                table: "ASLVideos");

            migrationBuilder.DropColumn(
                name: "OfferingId",
                table: "ASLVideos");

            migrationBuilder.DropColumn(
                name: "Shared",
                table: "ASLVideos");
            
            migrationBuilder.AddColumn<string>(
                name: "DownloadURL",
                table: "ASLVideos",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UniqueASLIdentifier",
                table: "ASLVideos",
                nullable: true);
            
            migrationBuilder.AddColumn<string>(
                name: "WebsiteURL",
                table: "ASLVideos",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DownloadURL",
                table: "ASLVideos");

            migrationBuilder.DropColumn(
                name: "UniqueASLIdentifier",
                table: "ASLVideos");
            
            migrationBuilder.DropColumn(
                name: "WebsiteURL",
                table: "ASLVideos");
            
            migrationBuilder.AddColumn<string>(
                name: "CourseId",
                table: "ASLVideos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Editable",
                table: "ASLVideos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Link",
                table: "ASLVideos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfferingId",
                table: "ASLVideos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Shared",
                table: "ASLVideos",
                type: "boolean",
                nullable: false,
                defaultValue: false);
            
            migrationBuilder.CreateIndex(
                name: "IX_ASLVideos_CourseId_OfferingId",
                table: "ASLVideos",
                columns: new[] { "CourseId", "OfferingId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ASLVideos_CourseOfferings_CourseId_OfferingId",
                table: "ASLVideos",
                columns: new[] { "CourseId", "OfferingId" },
                principalTable: "CourseOfferings",
                principalColumns: new[] { "CourseId", "OfferingId" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
