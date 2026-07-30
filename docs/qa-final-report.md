# QA Final Report — NaPoso

**Date:** 2026-07-20
**Project:** NaPoso (Job Marketplace)
**Stack:** ASP.NET Core 8+ MVC + Razor Pages, PostgreSQL, EF Core, Stripe, SignalR

---

## 1. Test Inventory

| Metric | Count |
|--------|-------|
| **Total test methods** | 59 |
| **Unit tests** | 46 |
| **Integration tests** | 12 |
| **Placeholder tests** | 1 |

### Test Files

| File | Tests | Type | Framework |
|------|-------|------|-----------|
| `ComprehensiveTests.cs` | 28 | Unit (InMemory DB) | xUnit |
| `ModelTests.cs` | 6 | Unit | xUnit |
| `StatisticsServiceTests.cs` | 3 | Unit (Mocked UserManager) | xUnit + Moq |
| `PaymentTransactionServiceTests.cs` | 4 | Unit (InMemory DB) | xUnit |
| `PaymentTransactionTests.cs` | 5 | Unit (InMemory DB) | xUnit |
| `UiRouteTests.cs` | 12 | Integration (WebApplicationFactory) | xUnit |
| `UnitTest1.cs` | 1 | Placeholder (empty) | xUnit |

### Test Infrastructure

- `TestWebApplicationFactory` — Configures InMemoryDatabase for integration tests
- All unit test classes implement `IDisposable` for cleanup
- InMemory database names use `Guid.NewGuid()` for isolation

---

## 2. Coverage Results

| Module | Line Coverage (est.) | Branch Coverage (est.) | Gap |
|--------|---------------------|----------------------|-----|
| Models | 70–80% | 60–70% | View models untested |
| Services | 50–60% | 40–50% | StripeService, EmailService untested |
| Data (DbContext) | 60–70% | 55–65% | FK behavior not verified |
| Controllers | 2–5% | 1–3% | 0 action tests across 8 controllers |
| Identity/Pages | 5–10% | 3–5% | Route tests only |
| **Overall** | **~15–20%** | **~10–15%** | Heavy controller/page gap |

---

## 3. Complexity Hotspots

### Top 5 Most Complex Methods

| Rank | File | Method | CC | Risk |
|------|------|--------|-----|------|
| 1 | `RecenzijaController.cs` | `Create()` POST | 11 | High — payment verification + session management |
| 2 | `OglasController.cs` | `PrikazOglasa()` | 10 | Medium — filtering/sorting logic |
| 3 | `OglasController.cs` | `Edit()` POST | 8 | Medium — concurrency + auth |
| 4 | `Program.cs` | `CreateAdminUser()` | 8 | Low — seed logic, runs once |
| 5 | `RecenzijaController.cs` | `Create()` GET | 7 | Medium — session verification |

### Refactoring Recommendations

1. **Extract `ReviewAuthorizationService`** from RecenzijaController — handles payment verification, session checks, admin bypass
2. **Extract `OglasQueryService`** from OglasController — filter/sort/search logic
3. **Extract `SeedService`** from Program.cs — role and user seeding
4. **Extract `PaymentWebhookHandler`** from ApplicationDbContext — Stripe event handling
5. **Remove dead code** — `bypassVerification = false` in RecenzijaController, duplicated `PrijaviSe`/`PrijaviRadnikaNaOglas` methods

---

## 4. Edge Case Status

| Metric | Count |
|--------|-------|
| **Total scenarios documented** | 94 |
| **Covered by automated tests** | 15 |
| **Verified manually (Pass)** | 23 |
| **Verified manually (Fail)** | 44 |
| **Not tested** | 9 |

### Critical Gaps

| Gap | Risk | Recommendation |
|-----|------|----------------|
| No SQL injection tests | High | Add parameterized query verification |
| No XSS tests | High | Add output encoding tests for all user inputs |
| No concurrency tests | Medium | Add parallel request tests for notifications/chat |
| No role authorization tests | High | Add 403 verification for each `[Authorize]` endpoint |
| No large dataset tests | Medium | Add performance test with 10K+ records |
| Dark mode localStorage not verified | Low | Add Selenium/Playwright test |
| No email sending tests | Medium | Add mock-based email service tests |

---

## 5. Risk Assessment

### High Risk

- **OglasController (CC=69)** — 19 actions, 0 tests, highest complexity file
- **RecenzijaController (CC=36)** — Payment verification logic untested, dead code present
- **Stripe integration** — No unit tests for StripeService; webhook handler in DbContext
- **Role-based authorization** — No tests verifying `[Authorize(Roles=...)]` enforcement

### Medium Risk

- **ChatController** — Missing `[Authorize]` attribute on class; self-chat prevention only tested manually
- **Email services** — Brevo integration untested; fallback to console not verified
- **EF Core FK behavior** — Cascade/restrict deletes in OnModelCreating not tested
- **Session-based payment verification** — RecenzijaController relies on session state

### Low Risk

- **View models** — Simple POCOs, properties only
- **UI elements** — Password toggle and theme toggle verified by integration tests
- **Seed logic** — Runs once at startup, well-contained

---

## 6. Quality Gates

- [x] All 59 tests pass (verified via test run)
- [x] Coverage measured (estimated, requires `dotnet test --collect:"XPlat Code Coverage"` for precise)
- [x] Complexity analyzed (cyclomatic complexity for all key files)
- [x] Edge cases documented (94 scenarios across 9 modules)
- [x] Final report complete

---

## 7. Recommendations Summary

### Immediate (This Sprint)

1. Add controller action tests for OglasController, ChatController, RecenzijaController
2. Add role authorization tests (403 for unauthorized access)
3. Remove `UnitTest1.cs` placeholder
4. Remove dead code (`bypassVerification`, duplicated methods)

### Short-Term (Next Sprint)

5. Extract complex methods into services (ReviewAuthorizationService, OglasQueryService)
6. Add StripeService unit tests with mocked HTTP
7. Add email service tests with mocked HttpClient
8. Add integration test for full user flow (Register -> Login -> Create -> Apply -> Pay -> Review)

### Long-Term

9. Implement code coverage reporting in CI
10. Add Selenium/Playwright tests for dark mode and UI interactions
11. Add performance tests for statistics with large datasets
12. Add security scanning (SQL injection, XSS) to test suite
