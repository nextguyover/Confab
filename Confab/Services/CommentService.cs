using Confab.Data;
using Confab.Data.DatabaseModels;
using Confab.Emails.TemplateSubstitution;
using Confab.Exceptions;
using Confab.Models;
using Confab.Models.AdminPanel.Statistics;
using Confab.Models.Moderation;
using Confab.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static Confab.Data.DatabaseModels.AutoModerationRuleSchema;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Confab.Services
{
    public class CommentService : ICommentService
    {
        public static bool ManualModerationEnabled = true;
        public static int MaxModQueueCommentCountPerUser = 5;

        public enum CommentEditMode { Disabled, DurationAfterCreation, WhileAwaitingModeration, Always }
        public static CommentEditMode EditMode = CommentEditMode.Disabled;
        public static int EditDurationAfterCreationMins = 15;

        public enum EditHistoryBadgeMode { None, Badge, Timestamp }
        public static EditHistoryBadgeMode HistoryBadgeMode = EditHistoryBadgeMode.Timestamp;
        public static bool ShowEditHistory = true;

        public static bool RateLimitingEnabled = true;
        public static int RateLimitingTimeDurationMins = 1440;
        public static int RateLimitingMaxCommentsPerTimeDuration = 5;

        public async Task<NewCommentCreated> Create(CommentCreate commentCreate, ICommentLocationService locationService, IEmailService emailService, HttpContext httpContext, DataContext dbCtx)
        {
            ValidateCommentContent(commentCreate.Content);

            UserSchema author = await UserService.GetUserFromJWT(httpContext, dbCtx);

            await UserService.EnsureNotBanned(author, dbCtx);

            CommentLocationSchema location = await locationService.GetLocation(dbCtx, commentCreate?.Location);
            if (location == null) {
                throw new UninitialisedLocationException();
            }

            await VerifyCommentingEnabled(author, location, dbCtx);

            CommentSchema newComment = new CommentSchema();
            newComment.Author = author;
            newComment.Location = location;
            newComment.Content = commentCreate.Content;
            newComment.CreationTime = DateTime.UtcNow;

            if (ManualModerationEnabled && author.Role != UserRole.Admin)
            {
                newComment.AwaitingModeration = true;
            } 
            else
            {
                newComment.AwaitingModeration = false;
                newComment.ModeratorApprovalTimestamp = DateTime.UtcNow;
            }

            if (!commentCreate.ParentCommentId.IsNullOrEmpty())
            {
                newComment.ParentComment = await dbCtx.Comments
                    .Include(c => c.ChildComments)
                    .Include(c => c.Author)
                    .Include(c => c.UpvotedUsers)
                    .Include(c => c.DownvotedUsers)
                    .SingleOrDefaultAsync(o => o.PublicId.Equals(commentCreate.ParentCommentId));

                if (newComment.ParentComment == null || (newComment.ParentComment.IsDeleted && author.Role != UserRole.Admin))
                    throw new InvalidCommentIdException();

                if (newComment.ParentComment.AwaitingModeration && author.Role != UserRole.Admin)
                    throw new CommentAwaitingModerationException();
            }

            newComment.PublicId = GenerateCommentId();

            (bool approved, string feedback) automod = await ValidateWithAutoModRules(newComment, emailService, dbCtx);
            if (!automod.approved)
            {
                throw new AutoModerationFailedException(automod.feedback);
            }

            dbCtx.Comments.Add(newComment);
            await dbCtx.SaveChangesAsync();

            await HandleNewCommentNotifications(newComment, emailService, dbCtx);

            return new NewCommentCreated { CommentId = newComment.PublicId };
        }

        private async Task HandleNewCommentNotifications(CommentSchema newComment, IEmailService emailService, DataContext dbCtx, UserSchema approvedByAdmin = null)
        {
            bool userEmailSent = false;
            if (newComment.ParentComment != null
                && newComment.ParentComment.Author != newComment.Author         //don't notify user if they're replying to themselves
                && !newComment.AwaitingModeration                               //don't notify parent if new comment is awaiting moderation
                && newComment.ParentComment.Author.ReplyNotificationsEnabled    //don't notify parent if they've disabled notifications
                && !newComment.ParentComment.Author.IsBanned                    //don't notify parent if they're banned
                && !newComment.ParentComment.Author.IsAnon                      //don't notify parent if they're anonymously commenting
                && (approvedByAdmin == null ||                                  //notify if approvedByAdmin is null (moderation is disabled)
                    (approvedByAdmin != null && newComment.ParentComment?.Author.Role != UserRole.Admin)) // or, moderation is enabled, and the parent comment author isn't an admin (we notify admins separately)
                && newComment.Location.UserNotifLocal                           // notify if reply notifications are enabled at the location
                && (await dbCtx.GlobalSettings.SingleAsync()).UserNotifGlobal)  // notify if reply notifications are enabled globally
            {
                emailService.SendEmailFireAndForget(new UserCommentReplyNotifTemplatingData
                {
                    UserEmail = newComment.ParentComment.Author.Email,
                    Username = UserService.GetUsername(newComment.ParentComment.Author),
                    UserProfilePicUrl = AllEmailsTemplatingScaffold.ConfabBackendApiUrl + "/user/get-profile-picture/" + newComment.ParentComment.Author.PublicId,
                    CommentDownvoteCount = 0.ToString(),
                    CommentUpvoteCount = 0.ToString(),
                    CommentLink = AllEmailsTemplatingScaffold.SiteUrl + newComment.Location.LocationStr + "#" + newComment.PublicId,
                    CommentText = newComment.Content,
                    CommentUsername = UserService.GetUsername(newComment.Author),
                    CommentProfilePicUrl = AllEmailsTemplatingScaffold.ConfabBackendApiUrl + "/user/get-profile-picture/" + newComment.Author.PublicId,
                    CommentCreationTime = newComment.CreationTime,
                    ParentCommentDownvoteCount = newComment.ParentComment.DownvotedUsers.Count.ToString(),
                    ParentCommentUpvoteCount = newComment.ParentComment.UpvotedUsers.Count.ToString(),
                    ParentCommentText = newComment.ParentComment.Content,
                    ParentCommentCreationTime = newComment.ParentComment.CreationTime,
                    NotifDisableLink = AllEmailsTemplatingScaffold.SiteUrl + newComment.Location.LocationStr + "?Confab_notification_settings=true",
                });
                userEmailSent = true;
            }

            //next, notify admins
            if (approvedByAdmin == null 
                && newComment.Location.AdminNotifLocal 
                && (await dbCtx.GlobalSettings.SingleAsync()).AdminNotifGlobal
                && newComment.Author.Role != UserRole.Admin)
            {
                List<UserSchema> admins = await dbCtx.Users.Where(u => u.Role == UserRole.Admin).ToListAsync();

                foreach (UserSchema admin in admins)
                {
                    if (!(userEmailSent && admin == newComment.ParentComment?.Author))  //don't send same admin duplicate emails
                    {
                        if (newComment.ParentComment != null)
                        {
                            emailService.SendEmailFireAndForget(new AdminCommentNotifTemplatingData
                            {
                                UserEmail = admin.Email,
                                Username = UserService.GetUsername(admin),
                                UserProfilePicUrl = AllEmailsTemplatingScaffold.ConfabBackendApiUrl + "/user/get-profile-picture/" + admin.Username,
                                CommentDownvoteCount = 0.ToString(),
                                CommentUpvoteCount = 0.ToString(),
                                CommentLink = AllEmailsTemplatingScaffold.SiteUrl + newComment.Location.LocationStr + "#" + newComment.PublicId,
                                CommentText = newComment.Content,
                                CommentUsername = UserService.GetUsername(newComment.Author),
                                CommentProfilePicUrl = AllEmailsTemplatingScaffold.SiteUrl + "/user/get-profile-picture/" + newComment.Author.PublicId,
                                CommentCreationTime = newComment.CreationTime,
                                ParentCommentDownvoteCount = newComment.ParentComment.DownvotedUsers.Count.ToString(),
                                ParentCommentUpvoteCount = newComment.ParentComment.UpvotedUsers.Count.ToString(),
                                ParentCommentText = newComment.ParentComment.Content,
                                CommentLocationInDb = newComment.Location.LocationStr,
                                CommentUserEmail = newComment.Author.Email,
                                CommentUserId = newComment.Author.PublicId,
                                ParentCommentProfilePicUrl = AllEmailsTemplatingScaffold.SiteUrl + "/user/get-profile-picture/" + newComment.ParentComment.Author.PublicId,
                                ParentCommentUserEmail = newComment.ParentComment.Author.Email,
                                ParentCommentUserId = newComment.ParentComment.Author.PublicId,
                                ParentCommentUsername = UserService.GetUsername(newComment.ParentComment.Author),
                                ParentCommentCreationTime = newComment.ParentComment.CreationTime
                            });
                        }
                        else
                        {
                            emailService.SendEmailFireAndForget(new AdminCommentNotifTopLvlTemplatingData
                            {
                                UserEmail = admin.Email,
                                Username = UserService.GetUsername(admin),
                                UserProfilePicUrl = AllEmailsTemplatingScaffold.ConfabBackendApiUrl + "/user/get-profile-picture/" + admin.Username,
                                CommentDownvoteCount = 0.ToString(),
                                CommentUpvoteCount = 0.ToString(),
                                CommentLink = AllEmailsTemplatingScaffold.SiteUrl + newComment.Location.LocationStr + "#" + newComment.PublicId,
                                CommentText = newComment.Content,
                                CommentUsername = UserService.GetUsername(newComment.Author),
                                CommentProfilePicUrl = AllEmailsTemplatingScaffold.SiteUrl + "/user/get-profile-picture/" + newComment.Author.PublicId,
                                CommentLocationInDb = newComment.Location.LocationStr,
                                CommentUserEmail = newComment.Author.Email,
                                CommentUserId = newComment.Author.PublicId,
                                CommentCreationTime = newComment.CreationTime,
                            });
                        }
                    }
                }
            }
        }

        private async Task VerifyCommentingEnabled(UserSchema user, CommentLocationSchema location, DataContext dbCtx)
        {
            if (user.Role == UserRole.Admin) return;

            if (location.LocalStatus != CommentLocationSchema.CommentingStatus.Enabled 
                || (await dbCtx.GlobalSettings.SingleAsync()).CommentingStatus != CommentLocationSchema.CommentingStatus.Enabled)
            {
                throw new CommentingNotEnabledException();
            }

            if (RateLimitingEnabled
                && (await dbCtx.Comments
                    .Where(c => c.Author == user)
                    .Where(c => c.CreationTime > DateTime.UtcNow.AddMinutes(-1 * RateLimitingTimeDurationMins))
                    .ToListAsync()).Count >= RateLimitingMaxCommentsPerTimeDuration)
            {
                throw new UserCommentRateLimitException();
            }

            if (ManualModerationEnabled && (await dbCtx.Comments
                    .Where(c => c.Author == user)
                    .Where(c => c.AwaitingModeration)
                    .ToListAsync()).Count >= MaxModQueueCommentCountPerUser)
            {
                throw new UserReachedModQueueMaxCountException();
            }
        }

        private static void ValidateCommentContent(string content)
        {
            if (content.Length < 3 || content.Length > 10000)    //TODO: make this user configurable
            {
                throw new InvalidCommentException();
            }
        }

        private static string GenerateCommentId()        //https://stackoverflow.com/a/1344258/9112181
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[8];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return "c_" + new string(stringChars);
        }

        public async Task<CommentingEnabled> CommentingEnabledAtLocation(CommentLocation commentLocation, ICommentLocationService locationService, HttpContext httpContext, DataContext dbCtx)
        {
            UserSchema currentUser = await UserService.GetUserFromJWT(httpContext, dbCtx);
            await UserService.EnsureNotBanned(currentUser, dbCtx);

            CommentLocationSchema location = await locationService.GetLocation(dbCtx, commentLocation?.Location);
            if (location == null)
            {
                throw new UninitialisedLocationException();
            }

            try
            {
                await VerifyCommentingEnabled(currentUser, location, dbCtx);

                return new CommentingEnabled { Enabled = true };
            }
            catch(Exception ex)
            {
                if(ex is CommentingNotEnabledException)
                {
                    return new CommentingEnabled { Enabled = false, Reason = "Commenting here is currently disabled" };
                }
                if (ex is UserCommentRateLimitException || ex is UserReachedModQueueMaxCountException)
                {
                    return new CommentingEnabled { Enabled = false, Reason = "You're doing that too much, try again later" };
                }

                throw;
            }
        }

        public async Task<List<Comment>> GetAtLocation(CommentGetAtLocation commentGetAtLocation, ICommentLocationService locationService, HttpContext httpContext, DataContext dbCtx)
        {
            UserSchema currentUser = null;
            try
            {
                currentUser = await UserService.GetUserFromJWT(httpContext, dbCtx);
            } catch { }

            CommentLocationSchema location = await locationService.GetLocation(dbCtx, commentGetAtLocation?.Location);
            if (location == null)
            {
                throw new UninitialisedLocationException();
            }

            // If the user is not an admin and the location is hidden, or global commenting is hidden, return an empty list
            if((currentUser?.Role) != UserRole.Admin && 
                (location.LocalStatus == CommentLocationSchema.CommentingStatus.Hidden || 
                (await dbCtx.GlobalSettings.SingleAsync()).CommentingStatus == CommentLocationSchema.CommentingStatus.Hidden))
            {
                return new List<Comment>();
            }

            List<CommentSchema> rootCommentsWithChildren = (await dbCtx.Comments
                .Include(c => c.Author)
                .Include(c => c.Location)
                .Include(c => c.UpvotedUsers)
                .Include(c => c.DownvotedUsers)
                .Include(c => c.ChildComments) // Eager load child comments (only a single level depth)
                .Where(c => c.ParentComment == null && c.Location.Equals(location))
                .ToListAsync());

            List<Comment> commentsFormatted = new List<Comment>();

            foreach (var rootComment in rootCommentsWithChildren)
            {
                Comment rootCommentWithRecursiveChildren = await MapCommentsRecursive(dbCtx, currentUser, rootComment);
                
                if(rootCommentWithRecursiveChildren != null)
                {
                    commentsFormatted.Add(rootCommentWithRecursiveChildren);
                }
            }

            switch (commentGetAtLocation.Sort)
            {
                case CommentSort.Upvotes:
                    commentsFormatted = commentsFormatted.OrderByDescending(c => c.Upvotes).ToList();
                    break;
                case CommentSort.Downvotes:
                    commentsFormatted = commentsFormatted.OrderByDescending(c => c.Downvotes).ToList();
                    break;
                case CommentSort.Newest:
                    commentsFormatted = commentsFormatted.OrderByDescending(c => c.CreationTime).ToList();
                    break;
                case CommentSort.Oldest:
                    commentsFormatted = commentsFormatted.OrderBy(c => c.CreationTime).ToList();
                    break;
            }

            return commentsFormatted;
        }

        private static async Task<Comment> MapCommentsRecursive(DataContext dbCtx, UserSchema currentUser, CommentSchema comment)
        {
            Comment commentFormatted = null;

            if ((!comment.IsDeleted && (!comment.AwaitingModeration || (comment.AwaitingModeration && comment.Author == currentUser))) || (currentUser?.Role == UserRole.Admin))
            {
                bool commentEdited;
                if (currentUser?.Role == UserRole.Admin)
                    commentEdited = comment.EditTime != DateTime.MinValue;
                else
                    commentEdited = comment.EditTime != DateTime.MinValue && comment.EditTime > comment.ModeratorApprovalTimestamp;

                commentFormatted = new Comment
                {
                    CommentId = comment.PublicId,
                    IsDeleted = comment.IsDeleted ? true : null,
                    AwaitingModeration = comment.AwaitingModeration ? ((currentUser.Role == UserRole.Admin || comment.Author == currentUser) ? true : null) : null,
                    IsBanned = currentUser?.Role == UserRole.Admin ? (await UserService.GetUserIsBanned(comment.Author, dbCtx) ? true : null) : null,
                    CanEdit = CommentEditAllowed(comment) ? (comment.Author == currentUser ? true : null) : null,
                    Content = comment.Content,
                    CreationTime = (currentUser?.Role == UserRole.Admin) ? comment.CreationTime : (comment.ModeratorApprovalTimestamp != DateTime.MinValue ? comment.ModeratorApprovalTimestamp : comment.CreationTime),
                    CommentEdited = !commentEdited ? null : (HistoryBadgeMode != EditHistoryBadgeMode.None ? true : (currentUser?.Role == UserRole.Admin ? true : null)),
                    EditTime = !commentEdited ? null : (HistoryBadgeMode == EditHistoryBadgeMode.Timestamp ? comment.EditTime : (currentUser?.Role == UserRole.Admin ? comment.EditTime : null)),
                    EditHistoryAvailable = GetCommentEditHistoryAvailable(comment, currentUser) ? true : null,
                    AuthorUsername = UserService.GetUsername(comment.Author),
                    AuthorId = comment.Author.PublicId,
                    IsAuthor = comment.Author == currentUser ? true : null,
                    IsAdmin = comment.Author.Role == UserRole.Admin ? true : null,
                    IsAnon = currentUser?.Role == UserRole.Admin ? (comment.Author.IsAnon ? true : null) : null,
                    Upvotes = comment.UpvotedUsers.Count == 0 ? null : comment.UpvotedUsers.Count,
                    Downvotes = comment.DownvotedUsers.Count == 0 ? null : comment.DownvotedUsers.Count,
                    UserVote = currentUser != null ? (currentUser.UpvotedComments.Contains(comment) ? Models.Vote.Upvote : (currentUser.DownvotedComments.Contains(comment) ? Models.Vote.Downvote : Models.Vote.None)) : null,
                };
            }
            else if (!comment.AwaitingModeration) commentFormatted = new Comment
            {
                CommentId = comment.PublicId,
                CreationTime = comment.CreationTime,
                Upvotes = comment.UpvotedUsers.Count == 0 ? null : comment.UpvotedUsers.Count,
                Downvotes = comment.DownvotedUsers.Count == 0 ? null : comment.DownvotedUsers.Count,
                IsDeleted = true,
            };

            if (!comment.AwaitingModeration || (comment.AwaitingModeration && (comment.Author == currentUser || currentUser?.Role == UserRole.Admin)))
            {
                foreach (var childComment in comment.ChildComments)
                {
                    CommentSchema nestedChild = (await dbCtx.Comments
                    .Include(c => c.Author)
                    .Include(c => c.Location)
                    .Include(c => c.UpvotedUsers)
                    .Include(c => c.DownvotedUsers)
                    .Include(c => c.ChildComments)
                    .SingleOrDefaultAsync(c => c.Id.Equals(childComment.Id)));

                    Comment childCommentFormatted = await MapCommentsRecursive(dbCtx, currentUser, nestedChild);
                    
                    if(childCommentFormatted != null)
                    {
                        commentFormatted.ChildComments.Add(childCommentFormatted);
                    }
                }
            }

            return commentFormatted;
        }

        private static bool GetCommentEditHistoryAvailable(CommentSchema comment, UserSchema currentUser)
        {
            if (currentUser?.Role == UserRole.Admin)
                return comment.EditTime != DateTime.MinValue;
            else
                return comment.EditTime != DateTime.MinValue && comment.EditTime > comment.ModeratorApprovalTimestamp && ShowEditHistory;
        }

        private static bool CommentEditAllowed(CommentSchema comment)
        {
            if (comment.Author.Role == UserRole.Admin) return true;

            if (!comment.Location.LocalEditingEnabled) return false;

            switch (EditMode)
            {
                case CommentEditMode.DurationAfterCreation:
                    return comment.CreationTime.AddMinutes(EditDurationAfterCreationMins) > DateTime.UtcNow;
                case CommentEditMode.WhileAwaitingModeration:
                    return comment.AwaitingModeration;
                case CommentEditMode.Always: 
                    return true;
                default:
                    return false;
            }
        }

        public async Task Vote(CommentVote commentVote, HttpContext httpContext, DataContext dbCtx)
        {
            CommentSchema comment = await dbCtx.Comments
                .Include(c => c.Location)
                .Include(c => c.UpvotedUsers)
                .Include(c => c.DownvotedUsers)
                .SingleOrDefaultAsync(c => c.PublicId == commentVote.CommentId);
               
            if(comment == null || comment.IsDeleted)
            {
                throw new InvalidCommentIdException();
            }

            if (comment.AwaitingModeration) throw new CommentAwaitingModerationException();

            UserSchema user = await UserService.GetUserFromJWT(httpContext, dbCtx);

            if (user == null)
            {
                throw new UserNotFoundException();
            }

            await UserService.EnsureNotBanned(user, dbCtx);

            if ((!comment.Location.LocalVotingEnabled || !(await dbCtx.GlobalSettings.SingleAsync()).VotingEnabled) && user.Role != UserRole.Admin)
            {
                throw new VotingNotEnabledException();
            }

            if (commentVote.VoteType == Models.Vote.Upvote)
            {
                comment.UpvotedUsers.Add(user);
                comment.DownvotedUsers.Remove(user);
            }
            else if (commentVote.VoteType == Models.Vote.Downvote)
            {
                comment.UpvotedUsers.Remove(user);
                comment.DownvotedUsers.Add(user);
            }
            else if (commentVote.VoteType == Models.Vote.None)
            {
                comment.UpvotedUsers.Remove(user);
                comment.DownvotedUsers.Remove(user);
            }
            else
            {
                throw new InvalidCommentVoteException();
            }

            dbCtx.Comments.Update(comment);
            await dbCtx.SaveChangesAsync();
        }

        public async Task Edit(CommentEdit commentEdit, HttpContext httpContext, IEmailService emailService, DataContext dbCtx)
        {
            CommentSchema comment = await dbCtx.Comments
                .Include(c => c.Location)
                .Include(c => c.Author)
                .Include(c => c.UpvotedUsers)
                .Include(c => c.DownvotedUsers)
                .SingleOrDefaultAsync(c => c.PublicId == commentEdit.CommentId);

            if (comment == null)
            {
                throw new InvalidCommentIdException();
            }

            if (!CommentEditAllowed(comment)) throw new CommentNotEditableException();

            UserSchema user = await UserService.GetUserFromJWT(httpContext, dbCtx);

            if (user == null)
            {
                throw new UserNotFoundException();
            }

            await UserService.EnsureNotBanned(user, dbCtx);

            if (comment.Author != user)
            {
                throw new InvalidAuthorizationException();
            }

            ValidateCommentContent(commentEdit.NewContent);

            CommentEditSchema newCommentEdit = new CommentEditSchema
            {
                SourceComment = comment,
                Content = comment.Content,
                VisibilityStartTime = comment.EditTime == DateTime.MinValue ? comment.CreationTime : comment.EditTime,
            };

            string editPreviousContent = comment.Content;

            comment.Content = commentEdit.NewContent;
            comment.EditTime = DateTime.UtcNow;

            (bool approved, string feedback) automod = await ValidateWithAutoModRules(comment, emailService, dbCtx, isEdit: true);
            if (!automod.approved)
            {
                throw new AutoModerationFailedException(automod.feedback);
            }

            dbCtx.CommentEdits.Add(newCommentEdit);
            dbCtx.Comments.Update(comment);
            await dbCtx.SaveChangesAsync();

            await HandleNewEditNotifications(comment, editPreviousContent, emailService, dbCtx);
        }

        private async Task HandleNewEditNotifications(CommentSchema comment, string editPreviousContent, IEmailService emailService, DataContext dbCtx)
        {
            if (!comment.AwaitingModeration
                && comment.Location.AdminNotifEditLocal
                && (await dbCtx.GlobalSettings.SingleAsync()).AdminNotifEditGlobal
                && comment.Author.Role != UserRole.Admin)
            {
                List<UserSchema> admins = await dbCtx.Users.Where(u => u.Role == UserRole.Admin).ToListAsync();

                foreach (UserSchema admin in admins)
                {
                    emailService.SendEmailFireAndForget(new AdminEditNotifTemplatingData
                    {
                        UserEmail = admin.Email,
                        Username = UserService.GetUsername(admin),
                        UserProfilePicUrl = AllEmailsTemplatingScaffold.ConfabBackendApiUrl + "/user/get-profile-picture/" + admin.Username,
                        CommentDownvoteCount = comment.DownvotedUsers.Count.ToString(),
                        CommentUpvoteCount = comment.UpvotedUsers.Count.ToString(),
                        CommentLink = AllEmailsTemplatingScaffold.SiteUrl + comment.Location.LocationStr + "#" + comment.PublicId,
                        CommentText = comment.Content,
                        CommentUsername = UserService.GetUsername(comment.Author),
                        CommentProfilePicUrl = AllEmailsTemplatingScaffold.SiteUrl + "/user/get-profile-picture/" + comment.Author.PublicId,
                        CommentCreationTime = comment.CreationTime,
                        CommentLocationInDb = comment.Location.LocationStr,
                        CommentUserEmail = comment.Author.Email,
                        CommentUserId = comment.Author.PublicId,
                        EditPreviousContent = editPreviousContent,
                    });
                }
            }
        }

        public async Task Delete(CommentId commentId, HttpContext httpContext, DataContext dbCtx)
        {
            CommentSchema comment = await dbCtx.Comments.SingleOrDefaultAsync(c => c.PublicId == commentId.Id);

            if (comment == null)
            {
                throw new InvalidCommentIdException();
            }

            if (comment.AwaitingModeration) throw new CommentAwaitingModerationException();

            UserSchema user = await UserService.GetUserFromJWT(httpContext, dbCtx);

            if (user == null)
            {
                throw new UserNotFoundException();
            }

            await UserService.EnsureNotBanned(user, dbCtx);

            if (comment.Author != user && user.Role != UserRole.Admin)
            {
                throw new InvalidAuthorizationException();
            }

            comment.IsDeleted = true;
            dbCtx.Comments.Update(comment);
            await dbCtx.SaveChangesAsync();
        }

        public async Task PermanentlyDelete(CommentId commentId, HttpContext httpContext, DataContext dbCtx)
        {
            CommentSchema comment = await dbCtx.Comments
                .Include(c => c.ChildComments)
                .SingleOrDefaultAsync(c => c.PublicId == commentId.Id);

            if (comment == null)
            {
                throw new InvalidCommentIdException();
            }

            await PermanentlyDeleteCommentTree(new List<CommentSchema> { comment }, dbCtx);
        }

        public async Task DeleteUserContent(UserPublicId userPublicId, HttpContext httpContext, DataContext dbCtx)
        {
            UserSchema currentUser = await UserService.GetUserFromJWT(httpContext, dbCtx);

            UserSchema userDeletionRequested = await dbCtx.Users.SingleOrDefaultAsync(o => o.PublicId.Equals(userPublicId.Id));

            if (currentUser == null || userDeletionRequested == null)
            {
                throw new UserNotFoundException();
            }

            await UserService.EnsureNotBanned(currentUser, dbCtx);

            if (currentUser.Role != UserRole.Admin)
            {
                throw new InvalidAuthorizationException();
            }

            await _DeleteUserContent(userDeletionRequested, dbCtx);
        }

        private async Task _DeleteUserContent(UserSchema deleteUser, DataContext dbCtx) {
            List<CommentSchema> allUserComments = (await dbCtx.Comments
                .Where(c => c.Author == deleteUser)
                .Where(c => !c.AwaitingModeration)
                .ToListAsync());

            foreach (CommentSchema comment in allUserComments)
            {
                comment.IsDeleted = true;
                dbCtx.Comments.Update(comment);
            }

            await dbCtx.SaveChangesAsync();

            await PermanentlyDeleteCommentTree(await dbCtx.Comments
                .Include(c => c.ChildComments)
                .Where(c => c.Author == deleteUser)
                .Where(c => c.AwaitingModeration)
                .ToListAsync(), dbCtx);
        }

        public async Task Undelete(CommentId commentId, HttpContext httpContext, DataContext dbCtx)
        {
            CommentSchema comment = await dbCtx.Comments.SingleOrDefaultAsync(c => c.PublicId == commentId.Id);

            if (comment == null)
            {
                throw new InvalidCommentIdException();
            }

            if (comment.AwaitingModeration) throw new CommentAwaitingModerationException();

            UserSchema user = await UserService.GetUserFromJWT(httpContext, dbCtx);

            if (user == null)
            {
                throw new UserNotFoundException();
            }

            await UserService.EnsureNotBanned(user, dbCtx);

            if (user.Role != UserRole.Admin)
            { 
                throw new InvalidAuthorizationException();
            }

            comment.IsDeleted = false;
            dbCtx.Comments.Update(comment);
            await dbCtx.SaveChangesAsync();
        }


        public async Task<List<CommentHistoryItem>> GetCommentHistory(CommentId commentId, HttpContext httpContext, DataContext dbCtx)
        {
            CommentSchema comment = await dbCtx.Comments.SingleOrDefaultAsync(c => c.PublicId == commentId.Id);

            if (comment == null)
            {
                throw new InvalidCommentIdException();
            }

            UserSchema user = null;
            try
            {
                user = await UserService.GetUserFromJWT(httpContext, dbCtx);
            }
            catch {}

            if(!ShowEditHistory && user?.Role != UserRole.Admin)
            {
                throw new EditHistoryDisabledException();
            }

            List<CommentEditSchema> commentEdits;

            commentEdits = await dbCtx.CommentEdits
            .Where(e => e.SourceComment == comment)
            .ToListAsync();

            List<CommentHistoryItem> commentEditsFormatted = new List<CommentHistoryItem>();
            for (int i = 0; i < commentEdits.Count; i++)
            {
                bool beforeModeratorApproval;
                if (i < commentEdits.Count - 1)
                {
                    beforeModeratorApproval = commentEdits[i + 1].VisibilityStartTime < comment.ModeratorApprovalTimestamp;
                }
                else
                {
                    beforeModeratorApproval = comment.EditTime < comment.ModeratorApprovalTimestamp;
                }

                if (beforeModeratorApproval == false || (beforeModeratorApproval == true && user?.Role == UserRole.Admin))
                {
                    commentEditsFormatted.Add(new CommentHistoryItem
                    {
                        Content = commentEdits[i].Content,
                        VisibilityStartTime = (user?.Role == UserRole.Admin || comment.AwaitingModeration) ? commentEdits[i].VisibilityStartTime : (commentEdits[i].VisibilityStartTime < comment.ModeratorApprovalTimestamp ? comment.ModeratorApprovalTimestamp : commentEdits[i].VisibilityStartTime),
                        BeforeModeratorApproval = beforeModeratorApproval ? true : null
                    });
                }
            }

            return commentEditsFormatted;
        }

        public async Task ModerationActionCommentAccept(HttpContext httpContext, CommentId commentId, IEmailService emailService, DataContext dbCtx)
        {
            CommentSchema comment = await dbCtx.Comments
                .Include(c => c.ParentComment)
                .Include(c => c.ParentComment.ChildComments)
                .Include(c => c.ParentComment.Author)
                .Include(c => c.ParentComment.UpvotedUsers)
                .Include(c => c.ParentComment.DownvotedUsers)
                .Include(c => c.Author)
                .Include(c => c.Location)
                .SingleOrDefaultAsync(c => c.PublicId == commentId.Id);

            if (comment == null || comment.AwaitingModeration == false)
            {
                throw new InvalidCommentIdException();
            }

            UserSchema admin = await UserService.GetUserFromJWT(httpContext, dbCtx);
            await UserService.EnsureNotBanned(admin, dbCtx);

            comment.AwaitingModeration = false;
            comment.ModeratorApprovalTimestamp = DateTime.UtcNow;

            GlobalSettingsSchema globalSettings = await dbCtx.GlobalSettings.SingleAsync();
            globalSettings.ModQueueLastCheckedTimestamp = DateTime.UtcNow;
            EmailService.ModQueueReminderSchedule.Reset();

            dbCtx.GlobalSettings.Update(globalSettings);
            dbCtx.Comments.Update(comment);
            await dbCtx.SaveChangesAsync();

            await HandleNewCommentNotifications(comment, emailService, dbCtx, approvedByAdmin: admin);
        }

        public async Task ModerationActionPermanentlyDeleteAllAwaitingApproval(UserPublicId userId, DataContext dbCtx)
        {
            UserSchema user = await dbCtx.Users.SingleOrDefaultAsync(o => o.PublicId.Equals(userId.Id));

            if (user == null)
            {
                throw new UserNotFoundException();
            }

            List<CommentSchema> awaitingComments = await dbCtx.Comments
                .Include(c => c.ChildComments)
                .Where(c => c.Author == user)
                .Where(c => c.AwaitingModeration)
                .ToListAsync();

            await PermanentlyDeleteCommentTree(awaitingComments, dbCtx);
        }

        private async Task PermanentlyDeleteCommentTree(List<CommentSchema> comments, DataContext dbCtx)
        {
            List<CommentSchema> allCommentsInTree = new List<CommentSchema>();

            foreach (CommentSchema comment in comments)
            {
                allCommentsInTree.AddRange(await GetCommentTreeRecursive(comment, dbCtx));
            }

            List<CommentEditSchema> commentEdits = new List<CommentEditSchema>();
            foreach (CommentSchema comment in allCommentsInTree)
            {
                commentEdits.AddRange(await dbCtx.CommentEdits.Where(e => e.SourceComment == comment).ToListAsync());
            }

            dbCtx.Comments.RemoveRange(allCommentsInTree);
            dbCtx.CommentEdits.RemoveRange(commentEdits);
            await dbCtx.SaveChangesAsync();
        }

        private async Task<List<CommentSchema>> GetCommentTreeRecursive(CommentSchema comment, DataContext dbCtx)
        {
            List<CommentSchema> commentList = new List<CommentSchema> { comment };

            foreach (CommentSchema childComment in comment.ChildComments)
            {
                CommentSchema nestedChild = (await dbCtx.Comments
                .Include(c => c.ChildComments)
                .SingleOrDefaultAsync(c => c.Id.Equals(childComment.Id)));

                commentList.AddRange(await GetCommentTreeRecursive(nestedChild, dbCtx));
            }

            return commentList;
        }

        public async Task<List<ModQueueAtLocation>> GetModerationQueue(DataContext dbCtx)
        {
            Dictionary<string, ModQueueAtLocation> modQueue = new Dictionary<string, ModQueueAtLocation>();

            //List<ModQueueAtLocation> modQueue = new List<ModQueueAtLocation>();

            List<CommentSchema> awaitingComments = await dbCtx.Comments
                .Include(c => c.Location)
                .Include(c => c.Author)
                .Include(c => c.ParentComment)
                .Include(c => c.ParentComment.Author)
                .Where(c => c.AwaitingModeration)
                .ToListAsync();

            foreach(CommentSchema comment in awaitingComments)
            {
                if(!modQueue.TryGetValue(comment.Location.LocationStr, out _))
                {
                    modQueue[comment.Location.LocationStr] = new ModQueueAtLocation
                    {
                        location = comment.Location.LocationStr
                    };
                }

                modQueue[comment.Location.LocationStr].comments.Add(new ModQueueComment
                {
                    Id = comment.PublicId,
                    CreationTime = comment.CreationTime,
                    Content = comment.Content,
                    EditTime = !(comment.EditTime != DateTime.MinValue) ? null : comment.EditTime,
                    AuthorId = comment.Author.PublicId,
                    AuthorUsername = UserService.GetUsername(comment.Author),
                    IsAnon = comment.Author.IsAnon,
                    ParentId = comment.ParentComment?.PublicId,
                    ParentCreationTime = comment.ParentComment?.CreationTime,
                    ParentContent = comment.ParentComment?.Content,
                    ParentAuthorId = comment.ParentComment?.Author.PublicId,
                    ParentAuthorUsername = comment.ParentComment == null ? null : UserService.GetUsername(comment.ParentComment?.Author),
                    ParentIsAnon = comment.ParentComment?.Author.IsAnon ?? false,
                });
            }

            return modQueue.Values.ToList();
        }

        public async Task<Statistics> GetStats(DataContext dbCtx)
        {
            Statistics stats = new Statistics();
            stats.TotalComments = (await dbCtx.Comments.ToListAsync()).Count;
            stats.NewComments_24h = (await dbCtx.Comments.Where(c => c.CreationTime > DateTime.UtcNow.AddHours(-24)).ToListAsync()).Count;
            stats.NewComments_7d = (await dbCtx.Comments.Where(c => c.CreationTime > DateTime.UtcNow.AddDays(-7)).ToListAsync()).Count;
            stats.NewComments_30d = (await dbCtx.Comments.Where(c => c.CreationTime > DateTime.UtcNow.AddDays(-30)).ToListAsync()).Count;
            stats.NewComments_1y = (await dbCtx.Comments.Where(c => c.CreationTime > DateTime.UtcNow.AddYears(-1)).ToListAsync()).Count;

            return stats;
        }

        public async Task<(bool Success, string UserFeedback)> ValidateWithAutoModRules(CommentSchema comment, IEmailService emailService, DataContext dbCtx, bool isEdit = false)
        {
            (bool Success, string UserFeedback) returnVal = (true, null);

            if (comment.Author.Role != UserRole.Admin)
            {
                List<AutoModerationRuleSchema> rules = await dbCtx.AutoModerationRules.OrderBy(r => r.Id).ToListAsync();

                foreach (AutoModerationRuleSchema rule in rules)
                {
                    if (Regex.Match(comment.Content, rule.FilterRegex).Success)
                    {
                        bool ruleEvaluated = false;

                        switch (rule.MatchAction)
                        {
                            case AutoModerationAction.BlockPosting:
                                returnVal = (false, null);
                                returnVal.UserFeedback = rule.ReturnError;
                                ruleEvaluated = true;
                                break;
                            case AutoModerationAction.Ban:
                            case AutoModerationAction.BanAndDeleteAll:
                                returnVal = (false, null);
                                await UserService.SetUserBanState(comment.Author, true, dbCtx);

                                if (rule.MatchAction == AutoModerationAction.BanAndDeleteAll)
                                    await _DeleteUserContent(comment.Author, dbCtx);

                                ruleEvaluated = true;
                                break;
                            case AutoModerationAction.SendToModQueue:
                                if (!isEdit)
                                {
                                    comment.AwaitingModeration = true;
                                    comment.ModeratorApprovalTimestamp = new DateTime();
                                    ruleEvaluated = true;
                                }
                                break;
                        }

                        if (rule.NotifyAdmins || rule.MatchAction == AutoModerationAction.Notify)
                        {
                            foreach (UserSchema admin in await dbCtx.Users.Where(u => u.Role == UserRole.Admin).ToListAsync())
                            {
                                emailService.SendEmailFireAndForget(new AdminAutoModNotifTemplatingData
                                {
                                    AutoModRuleAction = rule.MatchAction.ToString() + (rule.MatchAction == AutoModerationAction.BlockPosting ? " - " + rule.ReturnError : ""),
                                    AutoModRuleRegex = rule.FilterRegex,
                                    CommentLink = AllEmailsTemplatingScaffold.SiteUrl + comment.Location.LocationStr + "#" + comment.PublicId,
                                    CommentLocationInDb = comment.Location.LocationStr,
                                    CommentText = comment.Content,
                                    CommentUserEmail = comment.Author.Email,
                                    CommentUserId = comment.Author.PublicId,
                                    CommentUsername = UserService.GetUsername(comment.Author),
                                    CommentUpvoteCount = comment.UpvotedUsers.Count.ToString(),
                                    CommentDownvoteCount = comment.DownvotedUsers.Count.ToString(),
                                    CommentProfilePicUrl = AllEmailsTemplatingScaffold.ConfabBackendApiUrl + "/user/get-profile-picture/" + comment.Author.PublicId,
                                    CommentCreationTime = comment.CreationTime,
                                    UserEmail = admin.Email,
                                    Username = admin.Username,
                                    UserProfilePicUrl = AllEmailsTemplatingScaffold.ConfabBackendApiUrl + "/user/get-profile-picture/" + admin.PublicId,
                                });
                            }
                        }
                        if(ruleEvaluated)
                            break;  //skips evaluating following rules after a match
                    }
                }
            }

            return returnVal;
        }
    }
}
