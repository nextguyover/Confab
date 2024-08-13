using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confab.Migrations
{
    public partial class AddedUserRecordCreationTimestampField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RecordCreation",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecordCreation",
                table: "Users");
        }
    }
}
