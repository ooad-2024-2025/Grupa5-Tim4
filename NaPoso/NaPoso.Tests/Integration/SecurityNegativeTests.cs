using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace NaPoso.Tests;

public class SecurityNegativeTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityNegativeTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // 1. SQL Injection in search parameters
    [Fact]
    public async Task Oglas_Search_WithSqlInjection_ReturnsOkOrRedirect()
    {
        var response = await _client.GetAsync("/Oglas/PrikazOglasa?search=%27%20OR%201%3D1%20--");
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.Redirect);
    }

    // 2. XSS in search parameters
    [Fact]
    public async Task Oglas_Search_WithXssPayload_DoesNotReflectScript()
    {
        var response = await _client.GetAsync("/Oglas/PrikazOglasa?search=%3Cscript%3Ealert(1)%3C/script%3E");
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<script>alert(1)</script>", content);
    }

    // 3. Path traversal in document operations
    [Fact]
    public async Task Admin_DeleteDocument_WithPathTraversal_ReturnsBadRequest()
    {
        var content = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("fileName", "../../etc/passwd")
        });
        var response = await _client.PostAsync("/Admin/DeleteDocument", content);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                    response.StatusCode == HttpStatusCode.Redirect);
    }

    // 4. Path traversal with encoded dots
    [Fact]
    public async Task Admin_DeleteDocument_WithEncodedTraversal_ReturnsBadRequest()
    {
        var content = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("fileName", "..%2F..%2Fetc%2Fpasswd")
        });
        var response = await _client.PostAsync("/Admin/DeleteDocument", content);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                    response.StatusCode == HttpStatusCode.Redirect);
    }

    // 5. Empty filename
    [Fact]
    public async Task Admin_DeleteDocument_WithEmptyFilename_ReturnsBadRequest()
    {
        var content = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("fileName", "")
        });
        var response = await _client.PostAsync("/Admin/DeleteDocument", content);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                    response.StatusCode == HttpStatusCode.Redirect);
    }

    // 6. Chat access without auth
    [Fact]
    public async Task Chat_Index_ReturnsRedirect_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/Chat");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    // 7. Chat Poruke without auth
    [Fact]
    public async Task Chat_Poruke_ReturnsRedirect_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/Chat/Poruke/1");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    // 8. Recenzija Details without auth
    [Fact]
    public async Task Recenzija_Details_ReturnsRedirect_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/Recenzija/Details/1");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    // 9. POST without antiforgery token
    [Fact]
    public async Task Oglas_Create_WithoutToken_Returns400OrRedirect()
    {
        var content = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("Naslov", "Test"),
            new KeyValuePair<string, string>("Opis", "Test"),
            new KeyValuePair<string, string>("Lokacija", "Test"),
            new KeyValuePair<string, string>("TipPosla", "Test"),
            new KeyValuePair<string, string>("CijenaPosla", "100")
        });
        var response = await _client.PostAsync("/Oglas/Create", content);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                    response.StatusCode == HttpStatusCode.Redirect);
    }

    // 10. Invalid ID parameters (Details requires auth, so unauthenticated gets redirect)
    [Fact]
    public async Task Oglas_Details_WithNegativeId_ReturnsNotFoundOrRedirect()
    {
        var response = await _client.GetAsync("/Oglas/Details/-1");
        Assert.True(response.StatusCode == HttpStatusCode.NotFound ||
                    response.StatusCode == HttpStatusCode.Redirect);
    }

    // 11. Extremely long search string
    [Fact]
    public async Task Oglas_Search_WithVeryLongString_ReturnsOk()
    {
        var longSearch = new string('A', 10000);
        var response = await _client.GetAsync($"/Oglas/PrikazOglasa?search={Uri.EscapeDataString(longSearch)}");
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.Redirect);
    }

    // 12. Chat StartChat via POST without auth
    [Fact]
    public async Task Chat_StartChat_PostWithoutAuth_ReturnsRedirect()
    {
        var content = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("oglasId", "1"),
            new KeyValuePair<string, string>("korisnik2Id", "test")
        });
        var response = await _client.PostAsync("/Chat/StartChat", content);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
}
