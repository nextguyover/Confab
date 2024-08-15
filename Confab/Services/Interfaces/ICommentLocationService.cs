using Confab.Data;
using Confab.Data.DatabaseModels;
using Confab.Models.AdminPanel.CommentSettings;

namespace Confab.Services.Interfaces
{
    public interface ICommentLocationService
    {
        Task<CommentLocationSchema> CreateNewLocation(DataContext dbCtx, string locationString);
        //Task<LocalCommentSettings> GetLocalSettings(DataContext dbCtx, string locationString);
        Task<CommentLocationSchema> GetLocation(DataContext dbCtx, string locationString);
    }
}
