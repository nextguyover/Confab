using Confab.Data;
using Confab.Emails.TemplateSubstitution;

namespace Confab.Services.Interfaces
{
    public interface IEmailService
    {
        Task CheckAndSendModQueueReminder(DataContext dbCtx);
        Task<bool> SendEmail(AuthCodeTemplatingData data);
        Task<bool> SendEmail(AdminModQueueReminderTemplatingData template);
        Task<bool> SendEmail(AdminAutoModNotifTemplatingData template);
        Task<bool> SendEmail(AdminCommentNotifTemplatingData template);
        Task<bool> SendEmail(AdminCommentNotifTopLvlTemplatingData template);
        Task<bool> SendEmail(UserCommentReplyNotifTemplatingData template);
        Task<bool> SendEmail(AdminEditNotifTemplatingData template);
        void SendEmailFireAndForget(dynamic data);
    }
}
