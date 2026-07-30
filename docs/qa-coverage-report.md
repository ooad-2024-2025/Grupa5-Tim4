# Code Coverage Report — NaPoso

Coverage analysis based on test inventory. Exact line/branch coverage requires:
```
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

## Test Inventory

| Test File | Type | Tests | Category |
|-----------|------|-------|----------|
| `ComprehensiveTests.cs` | Unit (InMemory DB) | 28 | Model CRUD, DateTime, Payment, Chat, Statistics, Enums |
| `ModelTests.cs` | Unit | 6 | Model defaults, Enum values |
| `StatisticsServiceTests.cs` | Unit | 3 | StatisticsService with mocked UserManager |
| `PaymentTransactionServiceTests.cs` | Unit | 4 | PaymentTransactionService queries |
| `PaymentTransactionTests.cs` | Unit | 5 | HandleStripePaymentEvent idempotency |
| `UiRouteTests.cs` | Integration | 12 | HTTP route verification, UI elements |
| `UnitTest1.cs` | Unit | 1 | Placeholder (empty test) |
| **Total** | | **59** | |

---

## Coverage by Module

### Services

| File | Source Lines (est.) | Tests | Test Lines | Coverage Estimate | Gap |
|------|-------------------|-------|------------|-------------------|-----|
| `StripeService.cs` | 75 | 1 (indirect via ComprehensiveTests) | ~5 | 10–15% | No direct unit tests for CreateCheckoutSession/GetSession; IsConfigured only checked via enum test |
| `StatisticsService.cs` | 56 | 3 (dedicated) + 2 (ComprehensiveTests) | ~40 | 70–80% | Role counting via UserManager not tested (uses null UserManager) |
| `PaymentTransactionService.cs` | 44 | 4 (dedicated) | ~30 | 85–95% | All 4 public methods have direct tests; GetByUserIdAsync and GetByOglasIdAsync untested |
| `BrevoEmailService.cs` | ~80 | 0 | 0 | 0% | No tests for email sending |
| `BrevoEmailSender.cs` | ~60 | 0 | 0 | 0% | No tests for Identity email sender |
| `ConsoleEmailSender.cs` | ~15 | 0 | 0 | 0% | Trivial, low risk |

### Data

| File | Source Lines (est.) | Tests | Test Lines | Coverage Estimate | Gap |
|------|-------------------|-------|------------|-------------------|-----|
| `ApplicationDbContext.cs` | 129 | 15+ (via HandleStripePaymentEvent + OnModelCreating) | ~80 | 60–70% | HandleStripePaymentEvent well-tested; OnModelCreating FK configs only covered via integration tests |

### Controllers

| File | Source Lines (est.) | Tests | Test Lines | Coverage Estimate | Gap |
|------|-------------------|-------|------------|-------------------|-----|
| `HomeController.cs` | ~45 | 1 (UiRouteTests.HomePage) | ~3 | 15–20% | Only route tested; Admin/Radnik/Klijent role actions not tested |
| `OglasController.cs` | 544 | 1 (UiRouteTests间接) | ~2 | 5–10% | No action tests; 19 actions, 0 tested |
| `ChatController.cs` | 128 | 0 | 0 | 0% | No tests for any action |
| `AdminController.cs` | ~80 | 0 | 0 | 0% | No tests |
| `RecenzijaController.cs` | 302 | 0 | 0 | 0% | No tests; complex auth logic untested |
| `ObavijestController.cs` | ~120 | 0 | 0 | 0% | No tests |
| `ObavijestKorisnikuController.cs` | 275 | 0 | 0 | 0% | No tests for notification CRUD |
| `OglasKorisnikController.cs` | ~120 | 0 | 0 | 0% | No tests |

### Models

| File | Source Lines (est.) | Tests | Test Lines | Coverage Estimate | Gap |
|------|-------------------|-------|------------|-------------------|-----|
| `Korisnik.cs` | ~20 | 0 | 0 | 0% | Properties only; tested indirectly |
| `Oglas.cs` | ~30 | 5 (ComprehensiveTests) | ~20 | 80–90% | Default values, CRUD, filter tested |
| `Recenzija.cs` | ~15 | 2 (ComprehensiveTests) | ~5 | 60–70% | Defaults tested; validation not |
| `Obavijest.cs` | ~20 | 3 (ComprehensiveTests) | ~15 | 70–80% | Create, filter, mark-as-read tested |
| `OglasKorisnik.cs` | ~20 | 3 (ComprehensiveTests) | ~15 | 75–85% | Defaults, UTC, duplicate detection |
| `Chat.cs` | ~15 | 2 (ComprehensiveTests) | ~8 | 60–70% | Create, UTC tested |
| `Poruka.cs` | ~15 | 2 (ComprehensiveTests) | ~8 | 60–70% | Create, UTC tested |
| `PaymentTransaction.cs` | ~25 | 8 (ComprehensiveTests + PaymentTransactionTests) | ~50 | 90–95% | Extensively tested |
| `Statistika.cs` | ~25 | 3 (ModelTests + ComprehensiveTests) | ~15 | 80–90% | Defaults, values tested |
| `Enums.cs` | ~20 | 3 (ComprehensiveTests) | ~10 | 80–90% | All enum values verified |
| `VerifikovanView.cs` | ~10 | 0 | 0 | 0% | View model only |
| `RecenzijaViewModel.cs` | ~10 | 0 | 0 | 0% | View model only |
| `DokumentiKorisnika.cs` | ~10 | 0 | 0 | 0% | View model only |
| `AdminOglasView.cs` | ~15 | 0 | 0 | 0% | View model only |
| `ErrorViewModel.cs` | ~10 | 0 | 0 | 0% | Trivial |

### Identity / Razor Pages

| File | Source Lines (est.) | Tests | Test Lines | Coverage Estimate | Gap |
|------|-------------------|-------|------------|-------------------|-----|
| `Login.cshtml.cs` | ~60 | 1 (UiRouteTests) | ~3 | 10–15% | Route tested; auth logic not |
| `Register.cshtml.cs` | ~120 | 1 (UiRouteTests) | ~3 | 5–10% | Route tested; registration logic not |
| `Checkout.cshtml.cs` | ~50 | 0 | 0 | 0% | No tests |
| `Success.cshtml.cs` | ~40 | 0 | 0 | 0% | No tests |
| `Manage/Index.cshtml.cs` | ~80 | 0 | 0 | 0% | No tests |
| `Manage/ChangePassword.cshtml.cs` | ~50 | 0 | 0 | 0% | No tests |
| Other Manage pages | ~300 total | 0 | 0 | 0% | No tests |
| `ForgotPassword.cshtml.cs` | ~30 | 1 (UiRouteTests) | ~2 | 10% | Route only |

---

## Overall Coverage Estimates

| Metric | Estimate | Notes |
|--------|----------|-------|
| **Line Coverage (overall)** | ~15–20% | Heavy controller/page code untested |
| **Branch Coverage (overall)** | ~10–15% | Most branching in controllers has 0 test coverage |
| **Line Coverage (Models)** | ~70–80% | Well-tested via ComprehensiveTests |
| **Line Coverage (Services)** | ~50–60% | PaymentTransactionService strong, others weak |
| **Line Coverage (Controllers)** | ~2–5% | Only route-level tests via UiRouteTests |
| **Line Coverage (Data)** | ~60–70% | HandleStripePaymentEvent well-tested |

---

## Coverage Gap Priorities

### Critical Gaps (Must Address)

1. **OglasController.cs** — 544 lines, 0 action tests, CC=69
2. **RecenzijaController.cs** — 302 lines, 0 action tests, CC=36
3. **ChatController.cs** — 128 lines, 0 action tests
4. **Login/Register flows** — Core auth untested beyond route availability
5. **StripeService.cs** — No direct unit tests for Stripe interactions

### Important Gaps (Should Address)

6. **ObavijestKorisnikuController.cs** — Notification CRUD untested
7. **AdminController.cs** — Document approval workflow untested
8. **Payment Success/Cancel pages** — Post-payment flow untested
9. **Role-based authorization** — No tests verifying role enforcement
10. **EF Core cascade/restrict** — OnModelCreating FK behavior untested

### Low Priority Gaps

11. View models (VerifikovanView, AdminOglasView) — Properties only
12. ConsoleEmailSender — Trivial fallback
13. Error page — Standard

---

## Recommendations

1. **Add controller action tests** using `TestWebApplicationFactory` + authenticated requests
2. **Mock Stripe** in integration tests using Stripe's test mode or a wrapper interface
3. **Test role authorization** — verify 403 for unauthorized role access
4. **Add integration tests for full flows** — Register -> Login -> Create Oglas -> Apply -> Pay -> Review
5. **Remove `UnitTest1.cs`** — empty placeholder test adds noise
