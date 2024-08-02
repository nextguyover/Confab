using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confab.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutoModerationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FilterRegex = table.Column<string>(type: "TEXT", nullable: true),
                    ReturnError = table.Column<string>(type: "TEXT", nullable: true),
                    MatchAction = table.Column<short>(type: "INTEGER", nullable: false),
                    NotifyAdmins = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoModerationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommentLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LocationStr = table.Column<string>(type: "TEXT", nullable: true),
                    LocalStatus = table.Column<short>(type: "INTEGER", nullable: false),
                    LocalVotingEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LocalEditingEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AdminNotifLocal = table.Column<bool>(type: "INTEGER", nullable: false),
                    AdminNotifEditLocal = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserNotifLocal = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GlobalSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommentingStatus = table.Column<short>(type: "INTEGER", nullable: false),
                    VotingEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccountCreationEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccountLoginEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserAuthJwtValidityStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AdminNotifGlobal = table.Column<bool>(type: "INTEGER", nullable: false),
                    AdminNotifEditGlobal = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserNotifGlobal = table.Column<bool>(type: "INTEGER", nullable: false),
                    ModQueueLastCheckedTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PublicId = table.Column<string>(type: "TEXT", nullable: true),
                    Role = table.Column<short>(type: "INTEGER", nullable: false),
                    IsBanned = table.Column<bool>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    VerificationCode = table.Column<string>(type: "TEXT", nullable: true),
                    VerificationExpiry = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VerificationCodeAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    VerificationCodeEmailCount = table.Column<int>(type: "INTEGER", nullable: false),
                    VerificationCodeFirstEmail = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AccountCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastActive = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsernameChange = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReplyNotificationsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PublicId = table.Column<string>(type: "TEXT", nullable: true),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EditTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    AwaitingModeration = table.Column<bool>(type: "INTEGER", nullable: false),
                    ModeratorApprovalTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AuthorId = table.Column<int>(type: "INTEGER", nullable: true),
                    ParentCommentId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_CommentLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "CommentLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Comments_Comments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "Comments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Comments_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommentEdits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    VisibilityStartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceCommentId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentEdits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentEdits_Comments_SourceCommentId",
                        column: x => x.SourceCommentId,
                        principalTable: "Comments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommentSchemaUserSchema",
                columns: table => new
                {
                    DownvotedCommentsId = table.Column<int>(type: "INTEGER", nullable: false),
                    DownvotedUsersId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentSchemaUserSchema", x => new { x.DownvotedCommentsId, x.DownvotedUsersId });
                    table.ForeignKey(
                        name: "FK_CommentSchemaUserSchema_Comments_DownvotedCommentsId",
                        column: x => x.DownvotedCommentsId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommentSchemaUserSchema_Users_DownvotedUsersId",
                        column: x => x.DownvotedUsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentSchemaUserSchema1",
                columns: table => new
                {
                    UpvotedCommentsId = table.Column<int>(type: "INTEGER", nullable: false),
                    UpvotedUsersId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentSchemaUserSchema1", x => new { x.UpvotedCommentsId, x.UpvotedUsersId });
                    table.ForeignKey(
                        name: "FK_CommentSchemaUserSchema1_Comments_UpvotedCommentsId",
                        column: x => x.UpvotedCommentsId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommentSchemaUserSchema1_Users_UpvotedUsersId",
                        column: x => x.UpvotedUsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommentEdits_SourceCommentId",
                table: "CommentEdits",
                column: "SourceCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_AuthorId",
                table: "Comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_LocationId",
                table: "Comments",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ParentCommentId",
                table: "Comments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_PublicId",
                table: "Comments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommentSchemaUserSchema_DownvotedUsersId",
                table: "CommentSchemaUserSchema",
                column: "DownvotedUsersId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentSchemaUserSchema1_UpvotedUsersId",
                table: "CommentSchemaUserSchema1",
                column: "UpvotedUsersId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PublicId",
                table: "Users",
                column: "PublicId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoModerationRules");

            migrationBuilder.DropTable(
                name: "CommentEdits");

            migrationBuilder.DropTable(
                name: "CommentSchemaUserSchema");

            migrationBuilder.DropTable(
                name: "CommentSchemaUserSchema1");

            migrationBuilder.DropTable(
                name: "GlobalSettings");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "CommentLocations");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
