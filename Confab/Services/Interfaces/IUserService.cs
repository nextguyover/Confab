using Confab.Data;
using Confab.Data.DatabaseModels;
using Confab.Models;
using Confab.Models.AdminPanel.Statistics;
using Confab.Models.UserAuth;

namespace Confab.Services.Interfaces
{
    public interface IUserService
    {
        public Task<LoginResponse> Login(UserLogin userLogin, DataContext context, WebApplicationBuilder builder);
        public Task ChangeUsername(UsernameChange usernameChange, HttpContext httpContext, DataContext context);
        public Task<bool> UserIdExists(string publicUserId, DataContext context);
        public Task<bool> IsAdmin(HttpContext httpContext, DataContext context);
        public Task BanUser(UserPublicId userPublicId, HttpContext httpContext, DataContext context);
        public Task UnBanUser(UserPublicId userPublicId, HttpContext httpContext, DataContext context);
        public Task SendVerificationCode(UserLogin userLogin, IEmailService emailService, ICommentLocationService locationService, DataContext context, bool isDevelopment);

        public Task<User> GetCurrentUser(HttpContext httpContext, DataContext context);
        Task<Statistics> GetStats(DataContext context);
        Task<UserSchema> CreateNewUser(DataContext context, UserRole role, string email);
        Task SetReplyNotifications(UserReplyNotifications newData, HttpContext httpContext, DataContext context);
        Task<UserReplyNotifications> GetReplyNotifications(HttpContext httpContext, DataContext context);
        Task<bool> GetChangeUsernameAvailable(HttpContext httpContext, DataContext context);
    }
}
