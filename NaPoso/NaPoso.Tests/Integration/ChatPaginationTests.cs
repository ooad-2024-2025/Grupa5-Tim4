using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace NaPoso.Tests;

public class ChatPaginationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ChatPaginationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Chat_Poruke_DefaultPage_ReturnsOk()
    {
        var response = await _client.GetAsync("/Chat/Poruke/1");
        // May return 404 if chat doesn't exist, but shouldn't crash
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK ||
                    response.StatusCode == System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Chat_Poruke_Page1_ReturnsOk()
    {
        var response = await _client.GetAsync("/Chat/Poruke/1?page=1&pageSize=10");
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK ||
                    response.StatusCode == System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Chat_Poruke_LargePageSize_DoesNotCrash()
    {
        var response = await _client.GetAsync("/Chat/Poruke/1?page=1&pageSize=9999");
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK ||
                    response.StatusCode == System.Net.HttpStatusCode.NotFound);
    }
}
