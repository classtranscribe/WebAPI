using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class three : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserOffering_AspNetUsers_ApplicationUserId",
                table: "UserOffering");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOffering_Offerings_OfferingId",
                table: "UserOffering");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserOffering",
                table: "UserOffering");

            migrationBuilder.RenameTable(
                name: "UserOffering",
                newName: "UserOfferings");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "UserOfferings",
                newName: "IdentityRoleId");

            migrationBuilder.RenameIndex(
                name: "IX_UserOffering_OfferingId",
                table: "UserOfferings",
                newName: "IX_UserOfferings_OfferingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserOfferings",
                table: "UserOfferings",
                columns: new[] { "ApplicationUserId", "OfferingId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserOfferings_IdentityRoleId",
                table: "UserOfferings",
                column: "IdentityRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserOfferings_AspNetUsers_ApplicationUserId",
                table: "UserOfferings",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOfferings_AspNetRoles_IdentityRoleId",
                table: "UserOfferings",
                column: "IdentityRoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

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
                name: "FK_UserOfferings_AspNetUsers_ApplicationUserId",
                table: "UserOfferings");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOfferings_AspNetRoles_IdentityRoleId",
                table: "UserOfferings");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOfferings_Offerings_OfferingId",
                table: "UserOfferings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserOfferings",
                table: "UserOfferings");

            migrationBuilder.DropIndex(
                name: "IX_UserOfferings_IdentityRoleId",
                table: "UserOfferings");

            migrationBuilder.RenameTable(
                name: "UserOfferings",
                newName: "UserOffering");

            migrationBuilder.RenameColumn(
                name: "IdentityRoleId",
                table: "UserOffering",
                newName: "RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_UserOfferings_OfferingId",
                table: "UserOffering",
                newName: "IX_UserOffering_OfferingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserOffering",
                table: "UserOffering",
                columns: new[] { "ApplicationUserId", "OfferingId" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserOffering_AspNetUsers_ApplicationUserId",
                table: "UserOffering",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOffering_Offerings_OfferingId",
                table: "UserOffering",
                column: "OfferingId",
                principalTable: "Offerings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
