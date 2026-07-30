using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace NaPoso.Tests;

public class UiRouteTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UiRouteTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HomePage_ReturnsOk()
    {
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Login_ReturnsOk()
    {
        var response = await _client.GetAsync("/Identity/Account/Login");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Register_ReturnsOk()
    {
        var response = await _client.GetAsync("/Identity/Account/Register");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ForgotPassword_ReturnsOk()
    {
        var response = await _client.GetAsync("/Identity/Account/ForgotPassword");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AccessDenied_ReturnsOk()
    {
        var response = await _client.GetAsync("/Identity/Account/AccessDenied");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Lockout_ReturnsOk()
    {
        var response = await _client.GetAsync("/Identity/Account/Lockout");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Home_ReturnsHtml()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("NaPoso", content);
        Assert.Contains("data-theme", content);
    }

    [Fact]
    public async Task Login_ContainsPasswordToggle()
    {
        var response = await _client.GetAsync("/Identity/Account/Login");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("password-toggle", content);
        Assert.Contains("password-wrapper", content);
    }

    [Fact]
    public async Task Register_ContainsPasswordToggle()
    {
        var response = await _client.GetAsync("/Identity/Account/Register");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("password-toggle", content);
        Assert.Contains("password-wrapper", content);
    }

    [Fact]
    public async Task Layout_ContainsThemeToggle()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("theme-toggle", content);
        Assert.Contains("data-theme=\"light\"", content);
        Assert.Contains("data-theme=\"dark\"", content);
        Assert.Contains("data-theme=\"system\"", content);
    }

    [Fact]
    public async Task Layout_ContainsFlickerPrevention()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("naposo-theme", content);
    }

    [Fact]
    public async Task Layout_ContainsDesignTokens()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("tokens.css", content);
        Assert.Contains("themes.css", content);
        Assert.Contains("components.css", content);
    }
}
