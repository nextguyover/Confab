using System.Net;
using Xunit;
using Confab.Tests.Helpers;
using System.Text.Json;
using Confab.Models;
using Confab.Models.UserAuth;
using Confab.Emails.TemplateSubstitution;
using Confab.Tests.MockDependencies;

namespace Confab.Tests.IntegrationTests;

public class BasicTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public BasicTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LocationActivation()
    {
        var client = _factory.CreateClient();

        StringContent request;
        HttpResponseMessage response;

        request = HttpHelpers.JsonSerialize(new
        {
            Location = "/unitialised-location"
        });
        response = await client.PostAsync("/comment/get-at-location", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);   // attempting to get comments at non-initialised location should return 400

        request = HttpHelpers.JsonSerialize(new
        {
            Email = "user@example.com",
            Location = "/unitialised-location"
        });
        response = await client.PostAsync("/user/login", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);   // attempting user login at non-initialised location should return 400

        request = HttpHelpers.JsonSerialize(new
        {
            Email = "admin@example.com",
            Location = "/unitialised-location"
        });
        response = await client.PostAsync("/user/login", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);   // requesting auth code as admin

        request = HttpHelpers.JsonSerialize(new
        {
            Email = "admin@example.com",
            LoginCode = ((AuthCodeTemplatingData)MockEmailService.SentMessages["admin@example.com"].Last()).AuthCode,
            Location = "/unitialised-location"
        });
        response = await client.PostAsync("/user/login", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);   // logging in as admin

        string content = await response.Content.ReadAsStringAsync();
        string token = JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }).Token;

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        request = HttpHelpers.JsonSerialize(new
        {
            Location = "/unitialised-location",
            CommentingStatus = 0,
        });
        response = await client.PostAsync("/admin/settings/set-local", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);   // enabling commenting at location

        client.DefaultRequestHeaders.Clear();   // logging out admin

        request = HttpHelpers.JsonSerialize(new
        {
            Location = "/unitialised-location"
        });
        response = await client.PostAsync("/comment/get-at-location", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);   // attempting to get comments at now initialised location
    }

    [Fact]
    public async Task CommentCreation()
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

        request = HttpHelpers.JsonSerialize(new
        {
            Location = "/"
        });
        response = await client.PostAsync("/comment/get-at-location", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);   // creating new comment

        string content = await response.Content.ReadAsStringAsync();
        List<Comment> comments = JsonSerializer.Deserialize<List<Comment>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.True(comments.Count == 1);   // checking that comment was created correctly
        Assert.True(comments[0].Content == "Example comment");
    }
}