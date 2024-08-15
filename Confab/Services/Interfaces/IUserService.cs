using Confab.Data;
using Confab.Data.DatabaseModels;
using Confab.Models;
using Confab.Models.AdminPanel.Statistics;
using Confab.Models.UserAuth;
using System.Net;

namespace Confab.Services.Interfaces
{
    public interface IUserService
    {
        public Task<LoginResponse> Login(UserLogin userLogin, DataContext dbCtx);
        public Task<LoginResponse> AnonLogin(HttpContext clientIP, DataContext dbCtx);
        public Task ChangeUsername(UsernameChange usernameChange, HttpContext httpContext, DataContext dbCtx);
        public Task<bool> UserIdExists(string publicUserId, DataContext dbCtx);
        public Task<bool> IsAdmin(HttpContext httpContext, DataContext dbCtx);
        public Task BanUser(UserPublicId userPublicId, HttpContext httpContext, DataContext dbCtx);
        public Task UnBanUser(UserPublicId userPublicId, HttpContext httpContext, DataContext dbCtx);
        public Task SendVerificationCode(UserLogin userLogin, IEmailService emailService, ICommentLocationService locationService, DataContext dbCtx, bool isDevelopment);

        public Task<User> GetCurrentUser(HttpContext httpContext, DataContext dbCtx);
        Task<Statistics> GetStats(DataContext dbCtx);
        Task<UserSchema> CreateNewUser(DataContext dbCtx, UserRole role, string email);
        Task SetReplyNotifications(UserReplyNotifications newData, HttpContext httpContext, DataContext dbCtx);
        Task<UserReplyNotifications> GetReplyNotifications(HttpContext httpContext, DataContext dbCtx);
        Task<bool> GetChangeUsernameAvailable(HttpContext httpContext, DataContext dbCtx);
    }
}
