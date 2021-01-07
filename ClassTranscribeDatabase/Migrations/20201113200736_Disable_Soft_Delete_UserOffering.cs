using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Disable_Soft_Delete_UserOffering : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "UserOfferings", 
                keyColumn: "IsDeletedStatus",
                keyValue: 1
            );

            migrationBuilder.DropForeignKey(
                name: "FK_UserOfferings_AspNetRoles_IdentityRoleId",
                table: "UserOfferings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserOfferings",
                table: "UserOfferings");

            migrationBuilder.AlterColumn<string>(
                name: "IdentityRoleId",
                table: "UserOfferings",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserOfferings",
                table: "UserOfferings",
                columns: new[] { "ApplicationUserId", "OfferingId", "IdentityRoleId" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserOfferings_AspNetRoles_IdentityRoleId",
                table: "UserOfferings",
                column: "IdentityRoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserOfferings_AspNetRoles_IdentityRoleId",
                table: "UserOfferings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserOfferings",
                table: "UserOfferings");

            migrationBuilder.AlterColumn<string>(
                name: "IdentityRoleId",
                table: "UserOfferings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserOfferings",
                table: "UserOfferings",
                columns: new[] { "ApplicationUserId", "OfferingId" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserOfferings_AspNetRoles_IdentityRoleId",
                table: "UserOfferings",
                column: "IdentityRoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
