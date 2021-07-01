using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ClassTranscribeDatabase.Migrations
{
    public partial class Update_Offerings_Set_Published_True : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"Offerings\" " +
                "SET \"PublishStatus\" = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
