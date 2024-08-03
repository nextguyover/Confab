using Confab.Emails.TemplateSubstitution;
using Confab.Models.UserAuth;
using Confab.Tests.MockDependencies;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Confab.Tests.Helpers
{
    public class InitialisationHelpers
    {
        public static async Task Login(HttpClient client, string email, string location)
        {
            StringContent request;
            HttpResponseMessage response;

            request = HttpHelpers.JsonSerialize(new
            {
                Email = email,
                Location = location
            });
            response = await client.PostAsync("/user/login", request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);   // requesting auth code with admin account

            request = HttpHelpers.JsonSerialize(new
            {
                Email = email,
                LoginCode = ((AuthCodeTemplatingData)MockEmailService.SentMessages.Last()).AuthCode,
                Location = location
            });
            response = await client.PostAsync("/user/login", request);   // logging in with admin account
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            string token = JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }).Token;

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }

        public static async Task EnableCommentingAtLocation(HttpClient client, string location)
        {
            StringContent request;
            HttpResponseMessage response;

            await Login(client, "admin@example.com", "/");  //admin login

            request = HttpHelpers.JsonSerialize(new
            {
                Location = "/",
                CommentingStatus = 0,
            });
            response = await client.PostAsync("/admin/settings/set-local", request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            client.DefaultRequestHeaders.Clear();
        }
    }
}
