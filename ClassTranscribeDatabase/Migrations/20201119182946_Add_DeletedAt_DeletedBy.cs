using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Add_DeletedAt_DeletedBy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "WatchHistories",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "WatchHistories",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "WatchHistories",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Videos",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Videos",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Videos",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserOfferings",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "UserOfferings",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "UserOfferings",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Universities",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Universities",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Universities",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Transcriptions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Transcriptions",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Transcriptions",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Terms",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Terms",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Terms",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "TaskItems",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "TaskItems",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "TaskItems",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Subscriptions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Subscriptions",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Playlists",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Playlists",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Playlists",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Offerings",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Offerings",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Offerings",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Messages",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Messages",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Messages",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Medias",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Medias",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Medias",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Logs",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Logs",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Logs",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Images",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Images",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Images",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "FileRecords",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "FileRecords",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "FileRecords",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "EPubs",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "EPubs",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "EPubs",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Dictionaries",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Dictionaries",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Dictionaries",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Departments",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Departments",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Courses",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Courses",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Courses",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CourseOfferings",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "CourseOfferings",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "CourseOfferings",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Captions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Captions",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Captions",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc));    
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "WatchHistories");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "WatchHistories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserOfferings");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "UserOfferings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Terms");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Terms");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Offerings");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Offerings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Medias");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Medias");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "FileRecords");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "FileRecords");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EPubs");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "EPubs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Dictionaries");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Dictionaries");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CourseOfferings");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "CourseOfferings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Captions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Captions");
        }
    }
}
