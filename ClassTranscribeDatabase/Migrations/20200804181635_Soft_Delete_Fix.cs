using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Soft_Delete_Fix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseOfferings_Courses_CourseId",
                table: "CourseOfferings");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseOfferings_Offerings_OfferingId",
                table: "CourseOfferings");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_AspNetUsers_ApplicationUserId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOfferings_AspNetUsers_ApplicationUserId",
                table: "UserOfferings");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOfferings_Offerings_OfferingId",
                table: "UserOfferings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserOfferings",
                table: "UserOfferings");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_TaskItems_UniqueId_TaskType",
                table: "TaskItems");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Subscriptions_ResourceType_ResourceId_ApplicationUserId",
                table: "Subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseOfferings",
                table: "CourseOfferings");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "WatchHistories",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Videos",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "UserOfferings",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OfferingId",
                table: "UserOfferings",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "UserOfferings",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserOfferings",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Universities",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Transcriptions",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Terms",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AlterColumn<string>(
                name: "UniqueId",
                table: "TaskItems",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "TaskItems",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AlterColumn<string>(
                name: "ResourceId",
                table: "Subscriptions",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "Subscriptions",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Subscriptions",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Playlists",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Offerings",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Messages",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Medias",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Logs",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "FileRecords",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "EPubs",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "EPubChapters",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Dictionaries",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Departments",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Courses",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "CourseOfferings",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OfferingId",
                table: "CourseOfferings",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CourseId",
                table: "CourseOfferings",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CourseOfferings",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Captions",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "WatchHistories",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "Videos",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "UserOfferings",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "Universities",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "Transcriptions",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "Terms",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "TaskItems",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "Playlists",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "Offerings",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "Messages",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "Medias",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "Logs",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "FileRecords",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "EPubs",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "EPubChapters",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "Dictionaries",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "Courses",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "CourseOfferings",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.UpdateData(
                table: "Captions",
                keyColumn: "IsDeletedStatus",
                keyValue: 1,
                column: "DeletedAt",
                value: new DateTime(1900, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                );

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserOfferings",
                table: "UserOfferings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseOfferings",
                table: "CourseOfferings",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserOfferings_ApplicationUserId_OfferingId_IdentityRoleId_D~",
                table: "UserOfferings",
                columns: new[] { "ApplicationUserId", "OfferingId", "IdentityRoleId", "DeletedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_UniqueId_TaskType_DeletedAt",
                table: "TaskItems",
                columns: new[] { "UniqueId", "TaskType", "DeletedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ResourceType_ResourceId_ApplicationUserId_Del~",
                table: "Subscriptions",
                columns: new[] { "ResourceType", "ResourceId", "ApplicationUserId", "DeletedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferings_CourseId_OfferingId_DeletedAt",
                table: "CourseOfferings",
                columns: new[] { "CourseId", "OfferingId", "DeletedAt" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseOfferings_Courses_CourseId",
                table: "CourseOfferings",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseOfferings_Offerings_OfferingId",
                table: "CourseOfferings",
                column: "OfferingId",
                principalTable: "Offerings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_AspNetUsers_ApplicationUserId",
                table: "Subscriptions",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOfferings_AspNetUsers_ApplicationUserId",
                table: "UserOfferings",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOfferings_Offerings_OfferingId",
                table: "UserOfferings",
                column: "OfferingId",
                principalTable: "Offerings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseOfferings_Courses_CourseId",
                table: "CourseOfferings");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseOfferings_Offerings_OfferingId",
                table: "CourseOfferings");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_AspNetUsers_ApplicationUserId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOfferings_AspNetUsers_ApplicationUserId",
                table: "UserOfferings");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOfferings_Offerings_OfferingId",
                table: "UserOfferings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserOfferings",
                table: "UserOfferings");

            migrationBuilder.DropIndex(
                name: "IX_UserOfferings_ApplicationUserId_OfferingId_IdentityRoleId_D~",
                table: "UserOfferings");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_UniqueId_TaskType_DeletedAt",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_ResourceType_ResourceId_ApplicationUserId_Del~",
                table: "Subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseOfferings",
                table: "CourseOfferings");

            migrationBuilder.DropIndex(
                name: "IX_CourseOfferings_CourseId_OfferingId_DeletedAt",
                table: "CourseOfferings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "WatchHistories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserOfferings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Terms");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Offerings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Medias");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "FileRecords");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EPubs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EPubChapters");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Dictionaries");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CourseOfferings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Captions");

            migrationBuilder.AlterColumn<string>(
                name: "OfferingId",
                table: "UserOfferings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "UserOfferings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "UserOfferings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "UniqueId",
                table: "TaskItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResourceId",
                table: "Subscriptions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "Subscriptions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OfferingId",
                table: "CourseOfferings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CourseId",
                table: "CourseOfferings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "CourseOfferings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserOfferings",
                table: "UserOfferings",
                columns: new[] { "ApplicationUserId", "OfferingId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_TaskItems_UniqueId_TaskType",
                table: "TaskItems",
                columns: new[] { "UniqueId", "TaskType" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Subscriptions_ResourceType_ResourceId_ApplicationUserId",
                table: "Subscriptions",
                columns: new[] { "ResourceType", "ResourceId", "ApplicationUserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseOfferings",
                table: "CourseOfferings",
                columns: new[] { "CourseId", "OfferingId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CourseOfferings_Courses_CourseId",
                table: "CourseOfferings",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseOfferings_Offerings_OfferingId",
                table: "CourseOfferings",
                column: "OfferingId",
                principalTable: "Offerings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_AspNetUsers_ApplicationUserId",
                table: "Subscriptions",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOfferings_AspNetUsers_ApplicationUserId",
                table: "UserOfferings",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOfferings_Offerings_OfferingId",
                table: "UserOfferings",
                column: "OfferingId",
                principalTable: "Offerings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
