using System.Net;
using Xunit;
using Confab.Tests.Helpers;
using System.Text.Json;
using Confab.Models;
using Confab.Data;
using Confab.Data.DatabaseModels;

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

        await InitialisationHelpers.EnableCommentingAtLocation(client, "/unitialised-location");   // admin enables commenting at location

        client.DefaultRequestHeaders.Clear();

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

        await InitialisationHelpers.EnableCommentingAtLocation(client, "/");

        await InitialisationHelpers.Login(client, "user@example.com", "/");  //user login

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