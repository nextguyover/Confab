using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confab.Migrations
{
    public partial class ImplementedAnonymousCommenting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreationIPId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnon",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ClientIPs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IPAddressBytes = table.Column<byte[]>(type: "BLOB", maxLength: 16, nullable: false),
                    IsBanned = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientIPs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreationIPId",
                table: "Users",
                column: "CreationIPId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientIPs_IPAddressBytes",
                table: "ClientIPs",
                column: "IPAddressBytes",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_ClientIPs_CreationIPId",
                table: "Users",
                column: "CreationIPId",
                principalTable: "ClientIPs",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_ClientIPs_CreationIPId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ClientIPs");

            migrationBuilder.DropIndex(
                name: "IX_Users_CreationIPId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreationIPId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsAnon",
                table: "Users");
        }
    }
}
