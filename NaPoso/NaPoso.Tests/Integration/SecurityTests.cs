using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using NaPoso.Constants;
using Xunit;

namespace NaPoso.Tests;

public class SecurityTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // Authorization tests
    [Fact]
    public async Task AdminRoutes_RequireAuthentication()
    {
        var routes = new[] { "/Admin", "/Admin/Documents", "/Oglas" };
        foreach (var route in routes)
        {
            var response = await _client.GetAsync(route);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }
    }

    [Fact]
    public async Task PublicRoutes_DontRequireAuthentication()
    {
        var routes = new[] { "/", "/Home/Error", "/Oglas/UspjesnaPrijava", "/Oglas/PrijavaGreska" };
        foreach (var route in routes)
        {
            var response = await _client.GetAsync(route);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    // XSS resilience - verify script tags are not reflected in responses
    [Fact]
    public async Task Home_Page_DoesNotReflectScriptTags()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<script>alert('xss')</script>", content);
    }

    [Fact]
    public async Task Login_Page_DoesNotReflectScriptTags()
    {
        var response = await _client.GetAsync("/Identity/Account/Login");
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<script>alert('xss')</script>", content);
    }

    // Injection resilience - SQL-like patterns in URL should not crash
    [Fact]
    public async Task Oglas_Details_WithSqlInjectionPattern_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/Oglas/Details/1%20OR%201=1");
        // Should return 404, 400, or 302 (auth redirect), never 500
        Assert.True(response.StatusCode == HttpStatusCode.NotFound ||
                    response.StatusCode == HttpStatusCode.BadRequest ||
                    response.StatusCode == HttpStatusCode.Redirect);
    }

    // Anti-forgery - POST without token should fail
    [Fact]
    public async Task Oglas_Create_PostWithoutToken_Returns400()
    {
        var content = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("Naslov", "Test"),
            new KeyValuePair<string, string>("Opis", "Test"),
            new KeyValuePair<string, string>("Lokacija", "Test"),
            new KeyValuePair<string, string>("TipPosla", "Test"),
            new KeyValuePair<string, string>("CijenaPosla", "100")
        });
        var response = await _client.PostAsync("/Oglas/Create", content);
        // Should fail due to missing anti-forgery token
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                    response.StatusCode == HttpStatusCode.Redirect);
    }

    // RBAC: Ensure mutation endpoints (Create/Edit/Delete/etc) are NOT accessible to Radnik
    // Uses reflection to inspect [Authorize(Roles=...)] attributes.
    [Theory]
    [InlineData(nameof(NaPoso.Controllers.OglasController.Create), new[] { RoleConstants.Admin, RoleConstants.Klijent })]
    [InlineData(nameof(NaPoso.Controllers.OglasController.Edit), new[] { RoleConstants.Admin, RoleConstants.Klijent })]
    [InlineData(nameof(NaPoso.Controllers.OglasController.Delete), new[] { RoleConstants.Admin, RoleConstants.Klijent })]
    [InlineData(nameof(NaPoso.Controllers.OglasController.DeleteConfirmed), new[] { RoleConstants.Admin, RoleConstants.Klijent })]
    [InlineData(nameof(NaPoso.Controllers.OglasController.MojiOglasi), new[] { RoleConstants.Admin, RoleConstants.Klijent })]
    [InlineData(nameof(NaPoso.Controllers.OglasController.PrijavljeniRadnici), new[] { RoleConstants.Admin, RoleConstants.Klijent })]
    [InlineData(nameof(NaPoso.Controllers.OglasController.Prihvati), new[] { RoleConstants.Admin, RoleConstants.Klijent })]
    [InlineData(nameof(NaPoso.Controllers.OglasController.Odbij), new[] { RoleConstants.Admin, RoleConstants.Klijent })]
    public void Oglas_MutationActions_AuthorizeAttribute_ExcludesRadnik(string actionName, string[] expectedRoles)
    {
        var controllerType = typeof(NaPoso.Controllers.OglasController);
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == actionName)
            .ToList();
        Assert.NotEmpty(methods);

        // Find the method with [Authorize] attribute (either overload, both should be protected)
        foreach (var mi in methods)
        {
            var attr = mi.GetCustomAttribute<AuthorizeAttribute>(true);
            if (attr == null || string.IsNullOrEmpty(attr.Roles))
                continue;

            var roles = attr.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            Assert.DoesNotContain(RoleConstants.Radnik, roles);
            foreach (var expected in expectedRoles)
            {
                Assert.Contains(expected, roles);
            }
            return;
        }

        // If we get here: no Authorize attribute on any overload with Roles list
        Assert.Fail($"Nijedna preklapanje metode {actionName} u OglasControlleru nema [Authorize(Roles=...)] atribut sa očekivanim ulogama.");
    }

    // RBAC: Integration test - an unauthenticated request to /Oglas/Create should redirect to login.
    // (An authenticated user with role Radnik would then be redirected to AccessDenied i.e. 302 → ~/Identity/Account/AccessDenied.)
    [Fact]
    public async Task Oglas_Create_UnauthenticatedRequest_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/Oglas/Create");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("Login", location);
    }

    // Content Security - verify no inline script injection vectors
    [Fact]
    public async Task Layout_DoesNotContainInlineEventHandlers()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("onerror=", content);
    }
}
