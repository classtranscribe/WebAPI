using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Detailed_Task_Item : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Attempts",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "Retry",
                table: "TaskItems");

            migrationBuilder.AddColumn<string>(
                name: "AncestorTaskItemId",
                table: "TaskItems",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "TaskItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DebugMessage",
                table: "TaskItems",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                table: "TaskItems",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedCompletionAt",
                table: "TaskItems",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaId",
                table: "TaskItems",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OfferingId",
                table: "TaskItems",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OpaqueMessageRef",
                table: "TaskItems",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ParentTaskItemId",
                table: "TaskItems",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PercentComplete",
                table: "TaskItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PlaylistId",
                table: "TaskItems",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreviousAttemptTaskItemId",
                table: "TaskItems",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QueuedAt",
                table: "TaskItems",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemoteResultData",
                table: "TaskItems",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rule",
                table: "TaskItems",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "TaskItems",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaskStatusCode",
                table: "TaskItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "TaskItems",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VideoId",
                table: "TaskItems",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_TaskItems_MediaId",
                table: "TaskItems",
                column: "MediaId");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_TaskItems_OfferingId",
                table: "TaskItems",
                column: "OfferingId");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_TaskItems_OpaqueMessageRef",
                table: "TaskItems",
                column: "OpaqueMessageRef");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_TaskItems_PlaylistId",
                table: "TaskItems",
                column: "PlaylistId");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_TaskItems_Rule",
                table: "TaskItems",
                column: "Rule");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_TaskItems_UserId",
                table: "TaskItems",
                column: "UserId");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_TaskItems_VideoId",
                table: "TaskItems",
                column: "VideoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_TaskItems_MediaId",
                table: "TaskItems");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_TaskItems_OfferingId",
                table: "TaskItems");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_TaskItems_OpaqueMessageRef",
                table: "TaskItems");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_TaskItems_PlaylistId",
                table: "TaskItems");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_TaskItems_Rule",
                table: "TaskItems");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_TaskItems_UserId",
                table: "TaskItems");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_TaskItems_VideoId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "AncestorTaskItemId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "AttemptNumber",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "DebugMessage",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "EstimatedCompletionAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "MediaId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "OfferingId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "OpaqueMessageRef",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "ParentTaskItemId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "PercentComplete",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "PlaylistId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "PreviousAttemptTaskItemId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "QueuedAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "RemoteResultData",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "Rule",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "TaskStatusCode",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "VideoId",
                table: "TaskItems");

            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                table: "TaskItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Result",
                table: "TaskItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Retry",
                table: "TaskItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
