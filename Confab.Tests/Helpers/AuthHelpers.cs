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
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var locationService = scope.ServiceProvider.GetRequiredService<ICommentLocationService>();
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                var userLogin = new UserLogin { Email = email, Location = locationStr };

                await userService.SendVerificationCode(userLogin, emailService, locationService, context, true);
                userLogin.LoginCode = context.Users.Single(u => u.Email == email).VerificationCode;

                var loginResponse = await userService.Login(userLogin, context);

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {loginResponse.Token}");
            }

        }
    }
}
