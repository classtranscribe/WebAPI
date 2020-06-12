using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class TaskItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskItems",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(nullable: false),
                    LastUpdatedBy = table.Column<string>(nullable: true),
                    IsDeletedStatus = table.Column<int>(nullable: false),
                    UniqueId = table.Column<string>(nullable: false),
                    TaskType = table.Column<int>(nullable: false),
                    Attempts = table.Column<int>(nullable: false),
                    TaskParameters = table.Column<string>(nullable: true),
                    Result = table.Column<bool>(nullable: false),
                    Retry = table.Column<bool>(nullable: false),
                    ResultData = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItems", x => x.Id);
                    table.UniqueConstraint("AK_TaskItems_UniqueId_TaskType", x => new { x.UniqueId, x.TaskType });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskItems");
        }
    }
}
