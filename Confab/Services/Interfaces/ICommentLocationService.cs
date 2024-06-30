using Confab.Data;
using Confab.Data.DatabaseModels;
using Confab.Models.AdminPanel.CommentSettings;

namespace Confab.Services.Interfaces
{
    public interface ICommentLocationService
    {
        Task<CommentLocationSchema> CreateNewLocation(DataContext context, string locationString);
        //Task<LocalCommentSettings> GetLocalSettings(DataContext context, string locationString);
        Task<CommentLocationSchema> GetLocation(DataContext context, string locationString);
    }
}
