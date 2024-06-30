using Confab.Data;
using Confab.Data.DatabaseModels;
using Confab.Models;
using Confab.Models.AdminPanel.CommentSettings;
using Confab.Models.AdminPanel.Emails;
using Confab.Models.AdminPanel.PageDetection;

namespace Confab.Services.Interfaces
{
    public interface ISettingsService
    {
        Task<EmailSettings> GetEmailSettings(CommentLocation locationData, ICommentLocationService locationService, DataContext context);
        Task<GlobalCommentSettings> GetGlobalCommentSettings(DataContext context);
        Task<LocalCommentSettings> GetLocalCommentSettings(ICommentLocationService locationService, CommentLocation commentLocation, DataContext context);
        Task SetEmailSettings(EmailSettings newSettings, ICommentLocationService locationService, DataContext context);
        Task SetGlobalCommentSettings(DataContext context, GlobalCommentSettings newSettings);
        Task SetLocalCommentSettings(SetLocalCommentSettings newSettings, ICommentLocationService locationService, DataContext context);
        Task SignOutAllUsers(DataContext dbCtx);
    }
}
