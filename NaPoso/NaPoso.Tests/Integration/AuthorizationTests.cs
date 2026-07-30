using Microsoft.AspNetCore.Mvc.Testing;

namespace NaPoso.Tests.Integration;

public class AuthorizationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthorizationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Admin_Index_ReturnsRedirect_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/Admin");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Oglas_Index_ReturnsRedirect_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/Oglas");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Chat_Index_ReturnsOk_WhenNotAuthenticated()
    {
        // ChatController now requires authentication
        var response = await _client.GetAsync("/Chat");
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Recenzija_Index_ReturnsRedirect_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/Recenzija");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Home_Index_ReturnsOk_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/");

        response.EnsureSuccessStatusCode();
    }
}
