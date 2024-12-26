using Confab.Models.UserAuth;
using Confab.Data;
using Confab.Services.Interfaces;

namespace Confab.Tests.Helpers
{
    public class AuthHelpers
    {
        public static async Task Login(HttpClient client, CustomWebApplicationFactory<Program> factory, string email, string locationStr)
        {
            using (var scope = factory.Services.CreateScope())
            {
                IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                IEmailService emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                ICommentLocationService locationService = scope.ServiceProvider.GetRequiredService<ICommentLocationService>();
                DataContext dbCtx = scope.ServiceProvider.GetRequiredService<DataContext>();
                UserLogin userLogin = new UserLogin { Email = email, Location = locationStr };

                await userService.SendVerificationCode(userLogin, emailService, locationService, dbCtx, true);
                userLogin.LoginCode = dbCtx.Users.Single(u => u.Email == email).VerificationCode;

                LoginResponse loginResponse = await userService.Login(userLogin, null, dbCtx);

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {loginResponse.Token}");
            }

        }
    }
}
