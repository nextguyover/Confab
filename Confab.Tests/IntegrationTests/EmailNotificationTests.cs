using System.Net;
using Xunit;
using Confab.Tests.Helpers;
using System.Text.Json;
using Confab.Models;
using Confab.Emails.TemplateSubstitution;
using Confab.Tests.MockDependencies;
using Confab.Models.UserAuth;

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
            string userEmail = "EmailNotificationTestsAdminNotificationForNewTopLevelComment@example.com".ToLower();

            var client = _factory.CreateClient();

            StringContent request;
            HttpResponseMessage response;

            DbHelpers.EnableCommentingAtLocation(_factory, "");

            await AuthHelpers.Login(client, _factory, userEmail, "/");  //user login

            request = HttpHelpers.JsonSerialize(new
            {
                Location = "/",
                Content = "Example comment"
            });
            response = await client.PostAsync("/comment/new", request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);   // creating new comment

            string content = await response.Content.ReadAsStringAsync();
            string commentId = JsonSerializer.Deserialize<NewCommentCreated>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }).CommentId;

            Assert.True(((AdminCommentNotifTopLvlTemplatingData)MockEmailService.SentMessages["admin@example.com"].Last()).CommentLink.IndexOf(commentId) != -1);   // check if the admin received a notification
        }
    }
}
