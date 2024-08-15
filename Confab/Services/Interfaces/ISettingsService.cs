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
        Task<EmailSettings> GetEmailSettings(CommentLocation locationData, ICommentLocationService locationService, DataContext dbCtx);
        Task<GlobalCommentSettings> GetGlobalCommentSettings(DataContext dbCtx);
        Task<LocalCommentSettings> GetLocalCommentSettings(ICommentLocationService locationService, CommentLocation commentLocation, DataContext dbCtx);
        Task SetEmailSettings(EmailSettings newSettings, ICommentLocationService locationService, DataContext dbCtx);
        Task SetGlobalCommentSettings(DataContext dbCtx, GlobalCommentSettings newSettings);
        Task SetLocalCommentSettings(SetLocalCommentSettings newSettings, ICommentLocationService locationService, DataContext dbCtx);
        Task SignOutAllUsers(DataContext dbCtx);
    }
}
