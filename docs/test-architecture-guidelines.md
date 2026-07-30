# Test Architecture Guidelines — NaPoso

Standards and conventions for writing and maintaining tests in the NaPoso project.

---

## 1. Project Structure

```
NaPoso.Tests/
  Unit/                        # Isolated tests with mocked dependencies
  Integration/                 # Full-stack tests with TestWebApplicationFactory
  Fixtures/                    # Shared test infrastructure
  ComprehensiveTests.cs        # Broad unit tests (InMemory DB)
  ModelTests.cs                # Model default value tests
  StatisticsServiceTests.cs    # StatisticsService unit tests
  PaymentTransactionServiceTests.cs  # PaymentTransactionService tests
  PaymentTransactionTests.cs   # HandleStripePaymentEvent tests
  UiRouteTests.cs              # Integration route tests
  TestWebApplicationFactory.cs # Test server configuration
```

### Directory Responsibilities

| Directory | Purpose | DB Strategy | Dependencies |
|-----------|---------|-------------|--------------|
| `Unit/` | Test individual classes in isolation | InMemory or none | Mocked via Moq |
| `Integration/` | Test HTTP endpoints end-to-end | InMemory (via factory) | Real services, mocked external APIs |
| `Fixtures/` | Shared factories, builders, helpers | Configured per fixture | N/A |

---

## 2. Naming Convention

### Format

```
MethodName_StateUnderTest_ExpectedBehavior
```

### Examples

```csharp
// Good
[Fact]
public async Task CreatePayment_WhenValidInput_ReturnsSession() { }

[Fact]
public async Task IsPaid_WhenNoTransaction_ReturnsFalse() { }

[Fact]
public async Task HandleStripePaymentEvent_WhenDuplicateEvent_Ignored() { }

[Fact]
public async Task GetStatistics_WhenEmptyDb_ReturnsZeros() { }

// Bad
[Fact]
public void Test1() { }                    // No meaning

[Fact]
public void CreatePaymentWorks() { }       // Missing state

[Fact]
public void TestIsPaid() { }               // Missing expected behavior
```

### Prefixes by Test Type

| Prefix | Use For |
|--------|---------|
| `When` / `Given` | Preconditions |
| `Returns` / `Throws` | Expected outcome |
| `Creates` / `Updates` / `Deletes` | Side effects |

---

## 3. AAA Pattern (Arrange-Act-Assert)

Every test must follow the AAA pattern with clear separation.

```csharp
[Fact]
public async Task Oglas_Create_SetsDefaultStatus()
{
    // Arrange
    var oglas = new Oglas
    {
        Naslov = "Novi oglas",
        Opis = "Test opis",
        Lokacija = "Sarajevo",
        TipPosla = "IT",
        CijenaPosla = 500
    };

    // Act
    _context.Oglas.Add(oglas);
    await _context.SaveChangesAsync();

    // Assert
    var saved = await _context.Oglas.FirstAsync();
    Assert.Equal(Status.Neaktivan, saved.Status);
    Assert.Equal("Novi oglas", saved.Naslov);
}
```

### Rules

- **Arrange:** Set up test data, mocks, and expected values
- **Act:** Call exactly one method under test
- **Assert:** Verify expected outcomes with specific assertions
- **No assertions in Arrange** — setup only
- **No logic in Assert** — use direct assertions

---

## 4. Test Data Management

### Database Isolation

```csharp
// Use unique database names to prevent cross-test contamination
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;
_context = new ApplicationDbContext(options);
```

### Cleanup

```csharp
public class MyTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public MyTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    public void Dispose() => _context.Dispose();
}
```

### Rules

- Each test class implements `IDisposable` for cleanup
- Use `Guid.NewGuid()` for InMemory database names
- Never share mutable state between tests
- Each test must be independent and runnable in any order
- Use factories or builders for complex test data

### Test Data Builders (Recommended)

```csharp
public static class TestData
{
    public static Oglas CreateOglas(
        string naslov = "Test Oglas",
        Status status = Status.Aktivan,
        decimal cijena = 100) => new Oglas
    {
        Naslov = naslov,
        Opis = "Test opis",
        Lokacija = "Sarajevo",
        TipPosla = "IT",
        CijenaPosla = cijena,
        Status = status
    };

    public static PaymentTransaction CreateTransaction(
        PaymentStatus status = PaymentStatus.Paid,
        long amount = 1000) => new PaymentTransaction
    {
        StripePaymentIntentId = $"pi_{Guid.NewGuid():N}",
        StripeEventId = $"evt_{Guid.NewGuid():N}",
        Status = status,
        Amount = amount,
        Currency = "usd"
    };
}
```

---

## 5. Mocking Guidelines

### When to Mock

| Mock | Don't Mock |
|------|-----------|
| `IConfiguration` | The class under test |
| `IHttpContextAccessor` | `ApplicationDbContext` (use InMemory) |
| `UserManager<Korisnik>` | Domain models |
| `SignInManager<Korisnik>` | EF Core LINQ queries |
| External HTTP clients (Stripe, Brevo) | Value types |

### Mock Setup Pattern

```csharp
// UserManager mock setup
var store = new Mock<IUserStore<Korisnik>>();
var userManagerMock = new Mock<UserManager<Korisnik>>(
    store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

userManagerMock.Setup(u => u.GetRolesAsync(It.IsAny<Korisnik>()))
    .ReturnsAsync(new List<string> { "Klijent" });

// IConfiguration mock setup
var configMock = new Mock<IConfiguration>();
configMock.Setup(c => c["Stripe:SecretKey"]).Returns("sk_test_fake");
configMock.Setup(c => c["Stripe:WebhookSecret"]).Returns("whsec_test_fake");
```

### Rules

- Mock only what you must — prefer InMemory DB over mocking DbContext
- Verify mock interactions when testing side effects
- Use `It.IsAny<T>()` for flexible matching
- Use `It.Is<T>(predicate)` for specific matching
- Reset mocks between tests (or use new instances)

---

## 6. Fixtures

### TestWebApplicationFactory

```csharp
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove PostgreSQL DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add InMemory database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("NaPosoTestDb"));
        });
    }
}
```

### Usage in Integration Tests

```csharp
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
}
```

### Rules

- Reuse `TestWebApplicationFactory` across integration test classes via `IClassFixture<T>`
- Each test class gets its own `HttpClient` instance
- Configure external dependencies (Stripe, Email) to use test/dummy implementations
- Don't share state between test classes through the factory

---

## 7. Anti-Patterns to Avoid

### Thread.Sleep

```csharp
// BAD
Thread.Sleep(1000);
var result = await service.GetAsync();

// GOOD
var result = await service.GetAsync();
// Tests are async — no sleep needed
```

### DateTime.Now

```csharp
// BAD — non-deterministic
var now = DateTime.Now;
Assert.Equal(now.Hour, saved.CreatedAt.Hour);

// GOOD — deterministic
var now = DateTime.UtcNow;
Assert.Equal(DateTime.UtcNow.Date, saved.CreatedAt.Date);

// BETTER — abstract time for testability
public interface IClock { DateTime UtcNow { get; } }
```

### Random Without Seed

```csharp
// BAD — non-reproducible
var random = new Random();
var testId = random.Next(1000);

// GOOD — reproducible
var testId = 42; // Or use Guid.NewGuid() for isolation
```

### Tests Depending on Execution Order

```csharp
// BAD — depends on previous test
private static int _counter = 0;

[Fact]
public void Test1() { _counter++; }

[Fact]
public void Test2() { Assert.Equal(1, _counter); }

// GOOD — independent
[Fact]
public void Test1() { var x = 1; Assert.Equal(1, x); }

[Fact]
public void Test2() { var x = 2; Assert.Equal(2, x); }
```

### Mega Tests

```csharp
// BAD — tests 5 unrelated things
[Fact]
public void TestEverything()
{
    // Assert model defaults...
    // Assert CRUD...
    // Assert validation...
    // Assert authorization...
    // Assert edge cases...
}

// GOOD — focused tests
[Fact]
public void Oglas_DefaultStatus_IsNeaktivan() { }

[Fact]
public async Task Oglas_Create_SetsDefaultStatus() { }

[Fact]
public async Task Oglas_Delete_RemovesFromDb() { }
```

---

## 8. Assertion Guidelines

### Prefer Specific Assertions

```csharp
// BAD
Assert.True(result != null);
Assert.True(result.Status == PaymentStatus.Paid);

// GOOD
Assert.NotNull(result);
Assert.Equal(PaymentStatus.Paid, result.Status);
```

### Use Collection Assertions

```csharp
// BAD
var items = await _context.Oglas.ToListAsync();
Assert.Equal(3, items.Count);
Assert.Equal("A1", items[0].Naslov);

// GOOD
var items = await _context.Oglas.ToListAsync();
Assert.Equal(3, items.Count);
Assert.Single(items, o => o.Naslov == "A1");
Assert.All(items, o => Assert.Equal(Status.Aktivan, o.Status));
```

### Test Null/Empty Cases

```csharp
// Always test the null/empty path
[Fact]
public async Task GetByStripePaymentIntentId_ReturnsNull_WhenNotExists()
{
    var result = await _service.GetByStripePaymentIntentIdAsync("nonexistent");
    Assert.Null(result);
}
```

---

## 9. Integration Test Patterns

### Authenticated Requests

```csharp
// For testing authenticated endpoints
var client = factory.CreateClient();
var loginResponse = await client.PostAsync("/Identity/Account/Login", ...);
// ... capture cookie/token ...
client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);
```

### Testing POST Endpoints

```csharp
[Fact]
public async Task Create_Oglas_ReturnsRedirect()
{
    var client = _factory.CreateClient();
    var form = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("Naslov", "Test"),
        new KeyValuePair<string, string>("Opis", "Opis"),
        new KeyValuePair<string, string>("Lokacija", "Sarajevo"),
        new KeyValuePair<string, string>("TipPosla", "IT"),
        new KeyValuePair<string, string>("CijenaPosla", "100")
    });

    var response = await client.PostAsync("/Oglas/Create", form);
    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
}
```

---

## 10. Continuous Integration

### Required Checks

```yaml
# .github/workflows/test.yml
steps:
  - name: Run tests
    run: dotnet test --no-build --verbosity normal

  - name: Run tests with coverage
    run: dotnet test --collect:"XPlat Code Coverage"

  - name: Check coverage threshold
    run: |
      # Fail if line coverage < 20%
      # (current estimate is ~15-20%)
```

### Coverage Thresholds (Targets)

| Metric | Current | Target | Stretch |
|--------|---------|--------|---------|
| Line coverage | ~15–20% | 40% | 60% |
| Branch coverage | ~10–15% | 30% | 50% |
| Controller coverage | ~2–5% | 30% | 50% |
