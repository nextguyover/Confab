using Confab.Data.DatabaseModels;
using Confab.Data;
using Confab.Models;
using Confab.Models.AdminPanel.Statistics;
using Confab.Models.Moderation;

namespace Confab.Services.Interfaces
{
    public interface ICommentService
    {
        public Task<NewCommentCreated> Create(CommentCreate commentCreate, ICommentLocationService locationService, IEmailService emailService, HttpContext httpContext, DataContext dbCtx);
        public Task Vote(CommentVote commentVote, HttpContext httpContext, DataContext dbCtx);
        public Task<List<Comment>> GetAtLocation(CommentGetAtLocation commentGetAtLocation, ICommentLocationService locationService, HttpContext httpContext, DataContext dbCtx);
        public Task Edit(CommentEdit commentEdit, HttpContext httpContext, IEmailService emailService, DataContext dbCtx);
        public Task Delete(CommentId commentId, HttpContext httpContext, DataContext dbCtx);
        public Task DeleteUserContent(UserPublicId userPublicId, HttpContext httpContext, DataContext dbCtx);
        public Task Undelete(CommentId commentId, HttpContext httpContext, DataContext dbCtx);
        public Task<List<CommentHistoryItem>> GetCommentHistory(CommentId commentId, HttpContext httpContext, DataContext dbCtx);
        Task<Statistics> GetStats(DataContext dbCtx);
        Task ModerationActionCommentAccept(HttpContext httpContext, CommentId commentId, IEmailService emailService, DataContext dbCtx);
        Task PermanentlyDelete(CommentId commentId, HttpContext httpContext, DataContext dbCtx);
        Task ModerationActionPermanentlyDeleteAllAwaitingApproval(UserPublicId userId, DataContext dbCtx);
        Task<List<ModQueueAtLocation>> GetModerationQueue(DataContext dbCtx);
        Task<CommentingEnabled> CommentingEnabledAtLocation(CommentLocation commentLocation, ICommentLocationService locationService, HttpContext httpContext, DataContext dbCtx);
        //public Task<List<CommentDebugDto>> GetAll(DataContext dbCtx);        
    }
}
