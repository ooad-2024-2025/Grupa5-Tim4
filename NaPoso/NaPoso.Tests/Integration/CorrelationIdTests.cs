using Microsoft.AspNetCore.Mvc.Testing;

namespace NaPoso.Tests;

public class CorrelationIdTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CorrelationIdTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Response_ContainsCorrelationIdHeader()
    {
        var response = await _client.GetAsync("/");
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }

    [Fact]
    public async Task Request_CorrelationId_IsPropagated()
    {
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", "test-id-123");
        var response = await _client.GetAsync("/");
        Assert.Equal("test-id-123", response.Headers.GetValues("X-Correlation-ID").First());
    }
}
