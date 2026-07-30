using Microsoft.AspNetCore.Mvc.Testing;

namespace NaPoso.Tests;

public class RateLimitTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RateLimitTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MultipleRapidRequests_DoNotCrash()
    {
        for (int i = 0; i < 20; i++)
        {
            var response = await _client.GetAsync("/");
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK ||
                        response.StatusCode == System.Net.HttpStatusCode.TooManyRequests);
        }
    }
}
