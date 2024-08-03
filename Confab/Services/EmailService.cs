using MimeKit;
using MailKit.Net.Smtp;
using Confab.Services.Interfaces;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Confab.Emails;
using Confab.Emails.TemplateSubstitution;
using Confab.Emails.TemplateSubstitution.Interfaces;
using Confab.Data;
using Confab.Data.DatabaseModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;

namespace Confab.Services
{
    public class EmailService : IEmailService
    {
        public static string SmtpServer;
        public static int SmtpPort;
        public static bool UseTLS;

        public static SmptMailbox AuthCodeEmailsMailbox;
        public static SmptMailbox UserNotificationEmailsMailbox;
        public static SmptMailbox AdminNotificationEmailsMailbox;

        public static ModQueueReminderScheduleData ModQueueReminderSchedule;

        private static ILogger _logger;
        public static ILogger logger { set { _logger = value; } }

        public async Task CheckAndSendModQueueReminder(DataContext context)
        {
            _logger.LogDebug("Running periodic check for moderation queue reminders");

            List<CommentSchema> modQueueComments = await context.Comments.Where(c => c.AwaitingModeration).ToListAsync();

            if (modQueueComments.Count != 0)
            {
                GlobalSettingsSchema globalSettings = await context.GlobalSettings.SingleAsync();

                CommentSchema oldestComment = new CommentSchema { CreationTime = DateTime.MaxValue };
                foreach (CommentSchema comment in modQueueComments)
                {
                    if (comment.CreationTime < oldestComment.CreationTime)
                    {
                        oldestComment = comment;
                    }
                }

                DateTime newerTimestamp = globalSettings.ModQueueLastCheckedTimestamp > oldestComment.CreationTime ? globalSettings.ModQueueLastCheckedTimestamp : oldestComment.CreationTime;

                bool emailSent = false;

                foreach (ModQueueReminderScheduleDataItem sendTime in ModQueueReminderSchedule.Data)
                {
                    if (TimeSpan.FromHours(sendTime.Hours) < (DateTime.UtcNow - newerTimestamp) && !sendTime.Sent)
                    {
                        _logger.LogInformation("Triggered moderation reminder - scheduled inactivity hours:" + sendTime.Hours);

                        if (!emailSent)     //only send one reminder email at once
                        {
                            foreach (UserSchema admin in await context.Users.Where(u => u.Role == UserRole.Admin).ToListAsync())
                            {
                                if (await SendEmail(new AdminModQueueReminderTemplatingData
                                {
                                    ModQueueInactivityTime = newerTimestamp,
                                    ModQueueOldestItemAge = oldestComment.CreationTime,
                                    ModQueueReminderTime = DateTime.UtcNow - TimeSpan.FromHours(sendTime.Hours),  //this is a hacky method for a timespan to get correct friendly output from FormatTimeAgo
                                    ModQueueCount = modQueueComments.Count,
                                    UserEmail = admin.Email,
                                    Username = UserService.GetUsername(admin),
                                    UserProfilePicUrl = AllEmailsTemplatingScaffold.ConfabBackendApiUrl + "/user/get-profile-picture/" + admin.PublicId,
                                }))
                                {
                                    sendTime.Sent = true;
                                    emailSent = true;
                                }
                            }
                        }
                        else
                        {
                            sendTime.Sent = true;
                        }
                    }
                }
            }
            _logger.LogDebug("Periodic check for moderation queue reminders complete!");
        }

        public async void SendEmailFireAndForget(dynamic data)
        {
            try
            {
                await SendEmail(data);
            }
            catch(Exception ex)
            {
                _logger.LogError($"SendEmailFireAndForget failed with an exception: {ex}");
            }
        }

        public async Task<bool> SendEmail(AuthCodeTemplatingData template)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(AllEmailsTemplatingScaffold.ServiceName, AuthCodeEmailsMailbox.Address));
            message.To.Add(new MailboxAddress(template.UserEmail, template.UserEmail));
            message.Subject = $"Your login code for {AllEmailsTemplatingScaffold.ServiceName}";

            return await _SendEmail(template, message, AuthCodeEmailsMailbox);
        }

        public async Task<bool> SendEmail(AdminModQueueReminderTemplatingData template)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(AllEmailsTemplatingScaffold.ServiceName, AdminNotificationEmailsMailbox.Address));
            message.To.Add(new MailboxAddress(template.UserEmail, template.UserEmail));
            message.Subject = $"There are comments that require approval on {AllEmailsTemplatingScaffold.ServiceName}";

            return await _SendEmail(template, message, AdminNotificationEmailsMailbox);
        }

        public async Task<bool> SendEmail(AdminAutoModNotifTemplatingData template)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(AllEmailsTemplatingScaffold.ServiceName, AdminNotificationEmailsMailbox.Address));
            message.To.Add(new MailboxAddress(template.UserEmail, template.UserEmail));
            message.Subject = $"⚠ Automatic moderation alert for {AllEmailsTemplatingScaffold.ServiceName} ⚠";

            return await _SendEmail(template, message, AdminNotificationEmailsMailbox);
        }

        public async Task<bool> SendEmail(AdminCommentNotifTemplatingData template)
        {
            return await _SendAdminCommentNotif(template);
        }
        public async Task<bool> SendEmail(AdminCommentNotifTopLvlTemplatingData template)
        {
            return await _SendAdminCommentNotif(template);
        }
        private async Task<bool> _SendAdminCommentNotif(dynamic template)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(AllEmailsTemplatingScaffold.ServiceName, AdminNotificationEmailsMailbox.Address));
            message.To.Add(new MailboxAddress(template.UserEmail, template.UserEmail));
            message.Subject = $"New comment on {AllEmailsTemplatingScaffold.ServiceName}";

            return await _SendEmail(template, message, AdminNotificationEmailsMailbox);
        }

        public async Task<bool> SendEmail(AdminEditNotifTemplatingData template)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(AllEmailsTemplatingScaffold.ServiceName, AdminNotificationEmailsMailbox.Address));
            message.To.Add(new MailboxAddress(template.UserEmail, template.UserEmail));
            message.Subject = $"New edit to a comment on {AllEmailsTemplatingScaffold.ServiceName}";

            return await _SendEmail(template, message, UserNotificationEmailsMailbox);
        }

        public async Task<bool> SendEmail(UserCommentReplyNotifTemplatingData template)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(AllEmailsTemplatingScaffold.ServiceName, UserNotificationEmailsMailbox.Address));
            message.To.Add(new MailboxAddress(template.UserEmail, template.UserEmail));
            message.Subject = $"Reply to your comment on {AllEmailsTemplatingScaffold.ServiceName}";

            return await _SendEmail(template, message, UserNotificationEmailsMailbox);
        }

        protected virtual async Task<bool> _SendEmail(ITemplate emailTemplate, MimeMessage emailMessage, SmptMailbox fromMailbox) {
            string emailBody = emailTemplate.GetTemplateFile();
            emailTemplate.Substitute(ref emailBody);

            emailMessage.Body = new TextPart("html") { Text = emailBody };

            try
            {
                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(SmtpServer, SmtpPort, UseTLS);
                    await client.AuthenticateAsync(fromMailbox.Username, fromMailbox.Password);

                    await client.SendAsync(emailMessage);

                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"Email of type {emailTemplate} sent successfully to {emailMessage.To[0]}!");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending emailTemplate of type {emailTemplate} to {emailMessage.To[0]}: {ex}");
                return false;
            }
        }

        public static bool ValidateEmail(string email)
        {
            return Regex.IsMatch(email, "(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|\"(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21\\x23-\\x5b\\x5d-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])*\")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\\[(?:(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9]))\\.){3}(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9])|[a-z0-9-]*[a-z0-9]:(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21-\\x5a\\x53-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])+)\\])");   //TODO: change to a more robust regex
        }
    }
}
