using System.Net;
using Xunit;
using Confab.Tests.Helpers;
using System.Text.Json;
using Confab.Models;
using Confab.Emails.TemplateSubstitution;
using Confab.Tests.MockDependencies;
using Confab.Models.UserAuth;
using Confab.Services.Interfaces;

namespace Confab.Tests.IntegrationTests
{
    public class EmailNotificationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        public EmailNotificationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task AdminNotificationForNewTopLevelComment()
        {
            var client = _factory.CreateClient();

            StringContent request;
            HttpResponseMessage response;

            DbHelpers.EnableCommentingAtLocation(_factory, "");

            await AuthHelpers.Login(client, _factory, "user@example.com", "/");  //user login

            request = HttpHelpers.JsonSerialize(new
            {
                Location = "/",
                Content = "Example comment"
            });
            response = await client.PostAsync("/comment/new", request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);   // creating new comment

            string content = await response.Content.ReadAsStringAsync();
            string commentId = JsonSerializer.Deserialize<NewCommentCreated>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }).CommentId;

            bool adminNotified = false;
            foreach (var email in MockEmailService.SentMessages["admin@example.com"])
            {
                if (email is AdminCommentNotifTopLvlTemplatingData && ((AdminCommentNotifTopLvlTemplatingData)email).CommentLink.IndexOf(commentId) != -1)
                {
                    adminNotified = true;
                    break;
                }
            }
            Assert.True(adminNotified);
        }
    }
}
