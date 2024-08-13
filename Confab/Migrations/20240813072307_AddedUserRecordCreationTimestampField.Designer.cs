﻿// <auto-generated />
using System;
using Confab.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Confab.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20240813072307_AddedUserRecordCreationTimestampField")]
    partial class AddedUserRecordCreationTimestampField
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.20");

            modelBuilder.Entity("CommentSchemaUserSchema", b =>
                {
                    b.Property<int>("DownvotedCommentsId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DownvotedUsersId")
                        .HasColumnType("INTEGER");

                    b.HasKey("DownvotedCommentsId", "DownvotedUsersId");

                    b.HasIndex("DownvotedUsersId");

                    b.ToTable("CommentSchemaUserSchema");
                });

            modelBuilder.Entity("CommentSchemaUserSchema1", b =>
                {
                    b.Property<int>("UpvotedCommentsId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UpvotedUsersId")
                        .HasColumnType("INTEGER");

                    b.HasKey("UpvotedCommentsId", "UpvotedUsersId");

                    b.HasIndex("UpvotedUsersId");

                    b.ToTable("CommentSchemaUserSchema1");
                });

            modelBuilder.Entity("Confab.Data.DatabaseModels.AutoModerationRuleSchema", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("FilterRegex")
                        .HasColumnType("TEXT");

                    b.Property<short>("MatchAction")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("NotifyAdmins")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ReturnError")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("AutoModerationRules");
                });

            modelBuilder.Entity("Confab.Data.DatabaseModels.CommentEditSchema", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Content")
                        .HasColumnType("TEXT");

                    b.Property<int?>("SourceCommentId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("VisibilityStartTime")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("SourceCommentId");

                    b.ToTable("CommentEdits");
                });

            modelBuilder.Entity("Confab.Data.DatabaseModels.CommentLocationSchema", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("AdminNotifEditLocal")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("AdminNotifLocal")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("LocalEditingEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<short>("LocalStatus")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("LocalVotingEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<string>("LocationStr")
                        .HasColumnType("TEXT");

                    b.Property<bool>("UserNotifLocal")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("CommentLocations");
                });

            modelBuilder.Entity("Confab.Data.DatabaseModels.CommentSchema", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("AuthorId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("AwaitingModeration")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Content")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("EditTime")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("LocationId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("ModeratorApprovalTimestamp")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ParentCommentId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PublicId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("AuthorId");

                    b.HasIndex("LocationId");

                    b.HasIndex("ParentCommentId");

                    b.HasIndex("PublicId")
                        .IsUnique();

                    b.ToTable("Comments");
                });

            modelBuilder.Entity("Confab.Data.DatabaseModels.GlobalSettingsSchema", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("AccountCreationEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("AccountLoginEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("AdminNotifEditGlobal")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("AdminNotifGlobal")
                        .HasColumnType("INTEGER");

                    b.Property<short>("CommentingStatus")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("ModQueueLastCheckedTimestamp")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("UserAuthJwtValidityStart")
                        .HasColumnType("TEXT");

                    b.Property<bool>("UserNotifGlobal")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("VotingEnabled")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("GlobalSettings");
                });

            modelBuilder.Entity("Confab.Data.DatabaseModels.UserSchema", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("AccountCreation")
                        .HasColumnType("TEXT");

                    b.Property<string>("Email")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsBanned")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastActive")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastUsernameChange")
                        .HasColumnType("TEXT");

                    b.Property<string>("PublicId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("RecordCreation")
                        .HasColumnType("TEXT");

                    b.Property<bool>("ReplyNotificationsEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<short>("Role")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Username")
                        .HasColumnType("TEXT");

                    b.Property<string>("VerificationCode")
                        .HasColumnType("TEXT");

                    b.Property<int>("VerificationCodeAttempts")
                        .HasColumnType("INTEGER");

                    b.Property<int>("VerificationCodeEmailCount")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("VerificationCodeFirstEmail")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("VerificationExpiry")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("PublicId")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("CommentSchemaUserSchema", b =>
                {
                    b.HasOne("Confab.Data.DatabaseModels.CommentSchema", null)
                        .WithMany()
                        .HasForeignKey("DownvotedCommentsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Confab.Data.DatabaseModels.UserSchema", null)
                        .WithMany()
                        .HasForeignKey("DownvotedUsersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("CommentSchemaUserSchema1", b =>
                {
                    b.HasOne("Confab.Data.DatabaseModels.CommentSchema", null)
                        .WithMany()
                        .HasForeignKey("UpvotedCommentsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Confab.Data.DatabaseModels.UserSchema", null)
                        .WithMany()
                        .HasForeignKey("UpvotedUsersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Confab.Data.DatabaseModels.CommentEditSchema", b =>
                {
                    b.HasOne("Confab.Data.DatabaseModels.CommentSchema", "SourceComment")
                        .WithMany("CommentEdits")
                        .HasForeignKey("SourceCommentId");

                    b.Navigation("SourceComment");
                });

            modelBuilder.Entity("Confab.Data.DatabaseModels.CommentSchema", b =>
                {
                    b.HasOne("Confab.Data.DatabaseModels.UserSchema", "Author")
                        .WithMany("Comments")
                        .HasForeignKey("AuthorId");

                    b.HasOne("Confab.Data.DatabaseModels.CommentLocationSchema", "Location")
                        .WithMany("Comments")
                        .HasForeignKey("LocationId");

                    b.HasOne("Confab.Data.DatabaseModels.CommentSchema", "ParentComment")
                        .WithMany("ChildComments")
                        .HasForeignKey("ParentCommentId");

                    b.Navigation("Author");

                    b.Navigation("Location");

                    b.Navigation("ParentComment");
                });

            modelBuilder.Entity("Confab.Data.DatabaseModels.CommentLocationSchema", b =>
                {
                    b.Navigation("Comments");
                });

            modelBuilder.Entity("Confab.Data.DatabaseModels.CommentSchema", b =>
                {
                    b.Navigation("ChildComments");

                    b.Navigation("CommentEdits");
                });

            modelBuilder.Entity("Confab.Data.DatabaseModels.UserSchema", b =>
                {
                    b.Navigation("Comments");
                });
#pragma warning restore 612, 618
        }
    }
}
