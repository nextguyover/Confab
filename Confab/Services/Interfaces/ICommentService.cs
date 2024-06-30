using Confab.Data.DatabaseModels;
using Confab.Data;
using Confab.Models;
using Confab.Models.AdminPanel.Statistics;
using Confab.Models.Moderation;

namespace Confab.Services.Interfaces
{
    public interface ICommentService
    {
        public Task<NewCommentCreated> Create(CommentCreate commentCreate, ICommentLocationService locationService, IEmailService emailService, HttpContext httpContext, DataContext context);
        public Task Vote(CommentVote commentVote, HttpContext httpContext, DataContext context);
        public Task<List<Comment>> GetAtLocation(CommentGetAtLocation commentGetAtLocation, ICommentLocationService locationService, HttpContext httpContext, DataContext context);
        public Task Edit(CommentEdit commentEdit, HttpContext httpContext, IEmailService emailService, DataContext context);
        public Task Delete(CommentId commentId, HttpContext httpContext, DataContext context);
        public Task DeleteUserContent(UserPublicId userPublicId, HttpContext httpContext, DataContext context);
        public Task Undelete(CommentId commentId, HttpContext httpContext, DataContext context);
        public Task<List<CommentHistoryItem>> GetCommentHistory(CommentId commentId, HttpContext httpContext, DataContext context);
        Task<Statistics> GetStats(DataContext context);
        Task ModerationActionCommentAccept(HttpContext httpContext, CommentId commentId, IEmailService emailService, DataContext context);
        Task PermanentlyDelete(CommentId commentId, HttpContext httpContext, DataContext context);
        Task ModerationActionPermanentlyDeleteAllAwaitingApproval(UserPublicId userId, DataContext context);
        Task<List<ModQueueAtLocation>> GetModerationQueue(DataContext context);
        Task<CommentingEnabled> CommentingEnabledAtLocation(CommentLocation commentLocation, ICommentLocationService locationService, HttpContext httpContext, DataContext context);
        //public Task<List<CommentDebugDto>> GetAll(DataContext context);        
    }
}
