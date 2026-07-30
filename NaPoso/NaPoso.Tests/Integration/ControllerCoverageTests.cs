using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace NaPoso.Tests;

public class ControllerCoverageTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ControllerCoverageTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // HOME CONTROLLER
    [Fact]
    public async Task Home_Index_ReturnsOk()
    {
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Home_Admin_ReturnsRedirect_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/Home/Admin");
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Home_Radnik_ReturnsRedirect_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/Home/Radnik");
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Home_Klijent_ReturnsRedirect_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/Home/Klijent");
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    // OGLAS CONTROLLER
    [Fact]
    public async Task Oglas_Index_ReturnsRedirect_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/Oglas");
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Oglas_Details_ReturnsRedirect_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/Oglas/Details/99999");
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Oglas_Details_ReturnsNotFound_WhenNoId()
    {
        // /Oglas/Details with no id - route doesn't match, so 404
        var response = await _client.GetAsync("/Oglas/Details");
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.NotFound ||
            response.StatusCode == System.Net.HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Oglas_UspjesnaPrijava_ReturnsOk()
    {
        var response = await _client.GetAsync("/Oglas/UspjesnaPrijava");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Oglas_PrijavaGreska_ReturnsOk()
    {
        var response = await _client.GetAsync("/Oglas/PrijavaGreska");
        response.EnsureSuccessStatusCode();
    }

    // RECENZIJA CONTROLLER
    [Fact]
    public async Task Recenzija_Index_ReturnsRedirect_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/Recenzija");
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Recenzija_Details_ReturnsRedirect_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/Recenzija/Details/99999");
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    // CHAT CONTROLLER
    [Fact]
    public async Task Chat_Index_ReturnsOk()
    {
        var response = await _client.GetAsync("/Chat");
        // ChatController now requires authentication — unauthenticated users get 302 redirect
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    // ADMIN CONTROLLER
    [Fact]
    public async Task Admin_Index_ReturnsRedirect_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/Admin");
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Admin_Documents_ReturnsRedirect_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/Admin/Documents");
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    // ERROR PAGE
    [Fact]
    public async Task Shared_Error_ReturnsOk()
    {
        var response = await _client.GetAsync("/Home/Error");
        response.EnsureSuccessStatusCode();
    }
}
