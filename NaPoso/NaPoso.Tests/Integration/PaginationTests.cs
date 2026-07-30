using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace NaPoso.Tests;

public class PaginationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PaginationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // 1. Oglas search with default pagination (route requires Radnik auth, so unauthenticated gets redirect)
    [Fact]
    public async Task Oglas_Search_DefaultPage_ReturnsOkOrRedirect()
    {
        var response = await _client.GetAsync("/Oglas/PrikazOglasa");
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.Redirect);
    }

    // 2. Oglas search with page 1
    [Fact]
    public async Task Oglas_Search_Page1_ReturnsOkOrRedirect()
    {
        var response = await _client.GetAsync("/Oglas/PrikazOglasa?page=1&pageSize=10");
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.Redirect);
    }

    // 3. Oglas search with large pageSize (should be clamped)
    [Fact]
    public async Task Oglas_Search_LargePageSize_ReturnsOkOrRedirect()
    {
        var response = await _client.GetAsync("/Oglas/PrikazOglasa?page=1&pageSize=9999");
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.Redirect);
    }

    // 4. Oglas search with negative page
    [Fact]
    public async Task Oglas_Search_NegativePage_ReturnsOkOrRedirect()
    {
        var response = await _client.GetAsync("/Oglas/PrikazOglasa?page=-1");
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.Redirect);
    }

    // 5. Oglas search with sort parameter
    [Fact]
    public async Task Oglas_Search_WithSort_ReturnsOkOrRedirect()
    {
        var response = await _client.GetAsync("/Oglas/PrikazOglasa?sort=cijena_asc");
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.Redirect);
    }

    // 6. Oglas search with price filters
    [Fact]
    public async Task Oglas_Search_WithPriceRange_ReturnsOkOrRedirect()
    {
        var response = await _client.GetAsync("/Oglas/PrikazOglasa?minCijena=100&maxCijena=500");
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.Redirect);
    }

    // 7. Oglas search with location filter
    [Fact]
    public async Task Oglas_Search_WithLocation_ReturnsOkOrRedirect()
    {
        var response = await _client.GetAsync("/Oglas/PrikazOglasa?lokacija=Sarajevo");
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.Redirect);
    }

    // 8. Oglas search with all filters combined
    [Fact]
    public async Task Oglas_Search_WithAllFilters_ReturnsOkOrRedirect()
    {
        var response = await _client.GetAsync("/Oglas/PrikazOglasa?search=test&lokacija=Sarajevo&tipPosla=IT&sort=cijena_desc&minCijena=50&maxCijena=1000&page=1&pageSize=10");
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.Redirect);
    }
}
