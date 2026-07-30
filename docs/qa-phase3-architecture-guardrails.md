# Architecture Guardrails

## 1. Controller Rules

- Controllers MUST NOT contain business logic
- Controllers MUST delegate to services (`IOglasService`, `IStatisticsService`, etc.)
- Controllers MUST use async/await for all I/O operations
- Controllers MUST NOT access `ApplicationDbContext` directly — use services instead
  - **Exception**: `ChatController`, `RecenzijaController`, `AdminController`, `ObavijestKorisnikuController`, and `OglasKorisnikController` currently inject `ApplicationDbContext` directly. These should be migrated to services.
- Maximum controller LOC: 200 lines
- Controllers MUST use `[Bind]` attributes to prevent over-posting
- Controllers MUST NOT expose debug information (`TempData["Debug_*"]`) in production code
- `OglasController` — currently 384 lines. Consider extracting `KreirajPosao` and `InitiatePayment` to dedicated service methods.

## 2. Service Rules

- Services handle all DB access
- Services MUST use async EF Core APIs (`ToListAsync`, `FirstOrDefaultAsync`, `CountAsync`, etc.)
- Services MUST NOT reference `HttpContext` or HTTP concerns
  - **Exception**: `StripeService` injects `IHttpContextAccessor` to build redirect URLs — this is acceptable for payment services but should be documented.
- One service per bounded context (`OglasService`, `StatisticsService`, `PaymentTransactionService`)
- Service interfaces MUST be defined (e.g., `IOglasService`) for testability
- Services MUST NOT use `Console.WriteLine()` — use `ILogger<T>` instead
- Services MUST NOT swallow exceptions silently — propagate or log

## 3. Data Access Rules

- All DB calls MUST be async (`ToListAsync`, `FirstOrDefaultAsync`, `CountAsync`, `AnyAsync`, `SaveChangesAsync`)
- Synchronous EF Core calls are FORBIDDEN in request paths (e.g., `ToList()`, `Find()`, `SaveChanges()`)
- `DbContext` is scoped — never store in singletons or static fields
- DbContext MUST NOT be injected into controllers that have a corresponding service
- Use `[Bind]` to restrict model binding on write operations
- Raw SQL (`FromSqlRaw`, `ExecuteSqlRaw`) MUST NOT be used without parameterization
- Migrations MUST be run via `dotnet ef migrations` — never modify the database schema directly

## 4. Async Rules

- No `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` — these cause deadlocks in ASP.NET
- No `async void` (except UI event handlers)
- `CancellationToken` must be threaded through I/O-bound paths when feasible
- Independent async operations should use `Task.WhenAll`
  - **Note**: `StatisticsService.GetStatisticsAsync()` makes 4+ sequential count queries that could be parallelized
- Every `await` must be on an actual async method — no `await Task.CompletedTask`

## 5. Security Rules

- All POST actions MUST have `[ValidateAntiForgeryToken]`
  - **Exception**: `ChatController.PosaljiPoruku` currently missing — must be added
- All sensitive routes MUST have `[Authorize]` at the class or action level
  - **Exception**: `ChatController` (Index, StartChat, Poruke) and `ObavijestController.Create` currently missing — must be added
- No hardcoded secrets — use `IConfiguration` or environment variables
  - **Exception**: `appsettings.json` contains `DefaultConnection` with embedded credentials
- User input MUST be validated before use (model validation via Data Annotations)
- File paths constructed from user input MUST be sanitized (prevent path traversal)
  - **Exception**: `AdminController.DeleteDocument` uses unsanitized `fileName`
- Password policy MUST enforce minimum complexity in production (length >= 8, mixed case, digits)
- Sensitive data MUST NOT appear in logs at Information level
- `TempData["Debug_*"]` MUST be removed before production deployment

## 6. Testing Rules

- Every new service method MUST have unit tests
- Every new controller action MUST have integration tests
- Tests MUST be deterministic (no `DateTime.Now`, no network calls, no file system)
  - Use `DateTime.UtcNow` in code and inject/mock time in tests
- AAA pattern (Arrange/Act/Assert) is mandatory
- Tests MUST NOT depend on external services (Stripe, Brevo, PostgreSQL)
  - Use in-memory database (`UseInMemoryDatabase`) for EF Core tests
  - Use `WebApplicationFactory<T>` for integration tests
- Test coverage target: >80% for services, >60% for controllers

## 7. File Organization

| Directory | Purpose | Allowed In |
|-----------|---------|------------|
| `Services/` | Business logic, DB access | Service classes only |
| `Controllers/` | HTTP orchestration, input validation | Controller classes only |
| `Data/` | EF Core DbContext, migrations | `ApplicationDbContext` only |
| `Models/` | Domain entities, ViewModels, DTOs | POCO classes only |
| `Areas/Identity/` | Auth pages, payment pages | Razor Pages |
| `Views/` | Razor views, layouts | `.cshtml` files only |
| `wwwroot/` | Static assets (CSS, JS, images, documents) | Static files only |
| `Enums/` | Shared enumerations | Enum definitions only |
| `Properties/` | Launch settings | `launchSettings.json` only |

## 8. Naming Conventions

- Controllers: `{Feature}Controller` (e.g., `OglasController`, `ChatController`)
- Services: `{Feature}Service` / `I{Feature}Service` (e.g., `OglasService`, `IOglasService`)
- Models: PascalCase, singular (e.g., `Oglas`, `Korisnik`, `Recenzija`)
- ViewModels: `{Feature}ViewModel` or descriptive name (e.g., `RecenzijaViewModel`, `VerifikovanView`)
- DB Sets: PascalCase, plural or singular per convention (current: `Oglas`, `Korisnik`, `Chat`)
- Actions: PascalCase, descriptive (e.g., `PrikazOglasa`, `MojeRecenzije`, `PrijaviRadnikaNaOglas`)
- Route parameters: camelCase (e.g., `oglasId`, `radnikId`)

## Enforcement

- CI pipeline runs `dotnet build` and `dotnet test` on every PR
- Code review checklist includes architecture rules:
  - [ ] Controller delegates to service (no DbContext in controller)
  - [ ] All DB calls are async
  - [ ] POST actions have `[ValidateAntiForgeryToken]`
  - [ ] Sensitive routes have `[Authorize]`
  - [ ] No hardcoded secrets
  - [ ] No synchronous I/O in request paths
  - [ ] New code has corresponding tests
- PRs that violate rules must be rejected unless an exception is documented
- Periodic audits (monthly) should verify guardrails are still in effect
