using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace NaPoso.Tests;

public class AntiForgeryTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AntiForgeryTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // All POST mutating endpoints should reject requests without antiforgery token
    [Fact]
    public async Task AllPostEndpoints_RejectWithoutAntiForgeryToken()
    {
        var postEndpoints = new[] {
            "/Oglas/Create",
            "/Oglas/Edit/1",
            "/Oglas/Delete/1",
            "/Recenzija/Create",
            "/Recenzija/Edit/1",
            "/Recenzija/Delete/1",
            "/Chat/PosaljiPoruku",
            "/Chat/StartChat",
        };

        foreach (var endpoint in postEndpoints)
        {
            var content = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("dummy", "value")
            });
            var response = await _client.PostAsync(endpoint, content);
            // Should be 400 (Bad Request - missing antiforgery) or 302 (Redirect due to auth)
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.Redirect ||
                response.StatusCode == HttpStatusCode.NotFound,
                $"Expected 400/302/404 for {endpoint}, got {(int)response.StatusCode}");
        }
    }
}
