using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class NonnullAllJObjects : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"WatchHistories\" SET \"Json\" = '{}' WHERE \"Json\" IS NULL");
            migrationBuilder.Sql("UPDATE \"TaskItems\" SET \"TaskParameters\" = '{}' WHERE \"TaskParameters\" IS NULL");
            migrationBuilder.Sql("UPDATE \"TaskItems\" SET \"ResultData\" = '{}' WHERE \"ResultData\" IS NULL");
            migrationBuilder.Sql("UPDATE \"TaskItems\" SET \"RemoteResultData\" = '{}' WHERE \"RemoteResultData\" IS NULL");
            migrationBuilder.Sql("UPDATE \"Playlists\" SET \"JsonMetadata\" = '{}' WHERE \"JsonMetadata\" IS NULL");
            migrationBuilder.Sql("UPDATE \"Offerings\" SET \"JsonMetadata\" = '{}' WHERE \"JsonMetadata\" IS NULL");
            migrationBuilder.Sql("UPDATE \"Messages\" SET \"Payload\" = '{}' WHERE \"Payload\" IS NULL");
            migrationBuilder.Sql("UPDATE \"Medias\" SET \"JsonMetadata\" = '{}' WHERE \"JsonMetadata\" IS NULL");
            migrationBuilder.Sql("UPDATE \"Logs\" SET \"Json\" = '{}' WHERE \"Json\" IS NULL");
            migrationBuilder.Sql("UPDATE \"EPubs\" SET \"Cover\" = '{}' WHERE \"Cover\" IS NULL");
            migrationBuilder.Sql("UPDATE \"AspNetUsers\" SET \"Metadata\" = '{}' WHERE \"Metadata\" IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "Json",
                table: "WatchHistories",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaskParameters",
                table: "TaskItems",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResultData",
                table: "TaskItems",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RemoteResultData",
                table: "TaskItems",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "JsonMetadata",
                table: "Playlists",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "JsonMetadata",
                table: "Offerings",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Payload",
                table: "Messages",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "JsonMetadata",
                table: "Medias",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Json",
                table: "Logs",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Cover",
                table: "EPubs",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Metadata",
                table: "AspNetUsers",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Json",
                table: "WatchHistories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "TaskParameters",
                table: "TaskItems",
                type: "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "ResultData",
                table: "TaskItems",
                type: "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "RemoteResultData",
                table: "TaskItems",
                type: "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "JsonMetadata",
                table: "Playlists",
                type: "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "JsonMetadata",
                table: "Offerings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "Payload",
                table: "Messages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "JsonMetadata",
                table: "Medias",
                type: "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "Json",
                table: "Logs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "Cover",
                table: "EPubs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "Metadata",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string));
        }
    }
}
