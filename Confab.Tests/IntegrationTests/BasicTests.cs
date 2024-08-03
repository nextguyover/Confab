using System.Net;
using System.Text;
using System.Text.Json;
using Confab.Emails.TemplateSubstitution;
using ConfabTests.MockDependencies;
using Xunit;

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

        request = new StringContent(JsonSerializer.Serialize(new
        {
            Location = "/"
        }), Encoding.UTF8, "application/json");
        response = await client.PostAsync("/comment/get-at-location", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);   // attempting to get comments at non-initialised location should return 400

        request = new StringContent(JsonSerializer.Serialize(new
        {
            Email = "admin@example.com",     //doesn't work with the test appsettings.json email
            Location = "/"
        }), Encoding.UTF8, "application/json");
        response = await client.PostAsync("/user/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);   // requesting auth code with admin account

        request = new StringContent(JsonSerializer.Serialize(new
        {
            Email = "admin@example.com",
            LoginCode = ((AuthCodeTemplatingData)MockEmailService.SentMessages.Last()).AuthCode,
            Location = "/"
        }), Encoding.UTF8, "application/json");
        response = await client.PostAsync("/user/login", request);   // logging in with admin account

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}