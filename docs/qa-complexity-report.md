# Cyclomatic Complexity Report — NaPoso

Analysis of decision points in key C# source files.

## Scoring Guide

| Complexity | Rating | Action |
|-----------|--------|--------|
| 1–5 | Low | Simple, easy to test |
| 6–10 | Medium | Moderate branching, manageable |
| 11–15 | High | Consider refactoring |
| 16+ | Very High | Must refactor |

---

## 1. Program.cs (256 lines)

| Method | Decision Points | Complexity | Rating |
|--------|----------------|------------|--------|
| Top-level build/config | `if` (emailProvider), `if` (stripeSecretKey), `if` (IsDevelopment), `try/catch` (DB), `??` (connectionString), `??` (email provider) | 6 | Medium |
| `CreateRoles()` | `foreach` loop + `if (!await roleManager.RoleExistsAsync(role))` | 4 | Low |
| `CreateAdminUser()` | `if (adminUser == null)`, `if (result.Succeeded)`, `else` + `if (!await IsInRoleAsync)`, `foreach` (test users), `if (user == null)`, `if (result.Succeeded)` | 8 | Medium |
| Stripe webhook lambda | `if (stripeEvent.Type == "payment_intent.succeeded")`, `if (paymentIntent != null)`, `else if (payment_intent.payment_failed)`, `if (paymentIntent != null)` | 5 | Low |

**Total Program.cs complexity: ~23** (across all top-level statements and local functions)

**Recommendation:** Extract `CreateRoles()` and `CreateAdminUser()` into a dedicated `SeedService` class. The webhook lambda should be extracted to a minimal controller or endpoint class.

---

## 2. StripeService.cs (75 lines)

| Method | Decision Points | Complexity | Rating |
|--------|----------------|------------|--------|
| Constructor | `??` (fallback key lookup), `if (!string.IsNullOrWhiteSpace(_apiKey))` | 3 | Low |
| `CreateCheckoutSessionAsync()` | `if (!IsConfigured) return null` | 2 | Low |
| `GetSessionAsync()` | `if (!IsConfigured) return null` | 2 | Low |

**Total StripeService complexity: 7** — **Medium**

**Note:** Low complexity per method, but the service creates `new StripeClient` on every call (no DI for the Stripe client). Consider making it a singleton or caching.

---

## 3. StatisticsService.cs (56 lines)

| Method | Decision Points | Complexity | Rating |
|--------|----------------|------------|--------|
| `GetStatisticsAsync()` | `users.Count(u => ...Contains("Klijent"))` (LINQ + role check), `users.Count(u => ...Contains("Radnik"))` (LINQ + role check), `AnyAsync()` ternary for average rating, `Math.Round()` | 5 | Low |

**Total StatisticsService complexity: 5** — **Low**

**Performance concern:** Calls `_userManager.GetRolesAsync()` synchronously via `.Result` inside LINQ `Count()`. This will deadlock in certain synchronization contexts. Should be refactored to async materialization.

---

## 4. PaymentTransactionService.cs (44 lines)

| Method | Decision Points | Complexity | Rating |
|--------|----------------|------------|--------|
| `GetByStripePaymentIntentIdAsync()` | None (single query) | 1 | Low |
| `GetByUserIdAsync()` | None (single query) | 1 | Low |
| `GetByOglasIdAsync()` | None (single query) | 1 | Low |
| `IsPaidAsync()` | None (single query) | 1 | Low |

**Total PaymentTransactionService complexity: 4** — **Low**

Clean, simple query service. No refactoring needed.

---

## 5. ApplicationDbContext.cs (129 lines)

| Method | Decision Points | Complexity | Rating |
|--------|----------------|------------|--------|
| Constructor | None | 1 | Low |
| `HandleStripePaymentEventAsync()` | `if (alreadyProcessed) return`, `if (transaction == null)`, `if (newStatus == PaymentStatus.Paid)` (in new), `else`, `if (newStatus == PaymentStatus.Paid)` (in update) | 6 | Medium |
| `OnModelCreating()` | 10x `ToTable()`, 5x `HasOne/WithMany/OnDelete`, 3x `HasIndex` | 1 | Low |

**Total ApplicationDbContext complexity: 8** — **Medium**

`HandleStripePaymentEventAsync` is the most complex method in the data layer. The idempotency check + create-vs-update branch is reasonable. Consider extracting to a service to keep DbContext focused on configuration.

---

## 6. OglasController.cs (544 lines)

| Method | Decision Points | Complexity | Rating |
|--------|----------------|------------|--------|
| `Index()` | None | 1 | Low |
| `Details(int?)` | `if (id == null)`, `if (oglas == null)` | 3 | Low |
| `Create()` GET | None | 1 | Low |
| `Create()` POST | `if (!ModelState.IsValid)`, `foreach` (errors), `if (ModelState.IsValid)`, `if (userId == null)` | 5 | Low |
| `Edit()` GET | `if (id == null)`, `if (oglas.KlijentId != korisnikId && !IsInRole)`, `if (oglas == null)` | 4 | Low |
| `Edit()` POST | `if (oglasIzBaze == null)`, `if (oglasIzBaze.KlijentId != korisnikId && !IsInRole)`, `if (id != oglas.Id)`, `if (ModelState.IsValid)`, `try/catch (DbUpdateConcurrencyException)`, `if (!OglasExists)`, `else throw` | 8 | Medium |
| `Delete()` GET | `if (id == null)`, `if (oglas == null)`, `if (oglas.KlijentId != korisnikId && !IsInRole)` | 4 | Low |
| `DeleteConfirmed()` | `if (oglas.KlijentId != korisnikId && !IsInRole)`, `if (oglas != null)`, `if (IsInRole("Admin"))`, `else if (IsInRole("Klijent"))` | 5 | Low |
| `PrikazOglasa()` | `if (minCijena.HasValue && ...)`, `if (maxCijena.HasValue && ...)`, `if (!string.IsNullOrEmpty(search))`, `if (!string.IsNullOrEmpty(lokacija))`, `if (!string.IsNullOrEmpty(tipPosla))`, `if (minCijena.HasValue)`, `if (maxCijena.HasValue)`, `sort switch` (3 cases) | 10 | Medium |
| `OglasiKlijenta()` | None | 1 | Low |
| `PrijavljeniRadnici()` | `if (oglas == null)`, `if (oglas.KlijentId != userId)` | 3 | Low |
| `PrijaviRadnikaNaOglas()` | `if (oglas == null || ... != Aktivan || ... != null)`, `if (string.IsNullOrEmpty(userId))`, `if (postoji)`, `if (!string.IsNullOrEmpty(oglas.KlijentId))` | 5 | Low |
| `PrijaviSe()` | Same as PrijaviRadnikaNaOglas (duplicated logic) | 5 | Low |
| `Prihvati()` | `if (prijava == null)` | 2 | Low |
| `Odbij()` | `if (prijava == null || oglas == null)`, `if (prijava.Oglas.KlijentId != userId)` | 3 | Low |
| `InitiatePayment()` | `if (oglas == null)` | 2 | Low |
| `KreirajPosao()` POST | `if (ModelState.IsValid)`, `if (klijent == null)` | 3 | Low |

**Total OglasController complexity: ~69** — **Very High**

**Recommendation:** This is the most complex file. Key refactoring targets:
- `PrikazOglasa()` (CC=10): Extract filtering/sorting into a query builder service
- `PrijaviRadnikaNaOglas()` and `PrijaviSe()` are nearly identical — extract to shared method
- `Edit()` POST: Extract the authorization + concurrency handling
- Overall: Split into `OglasCrudController`, `OglasApplicationController`, `OglasSearchController`

---

## 7. ChatController.cs (128 lines)

| Method | Decision Points | Complexity | Rating |
|--------|----------------|------------|--------|
| `Index()` | None (LINQ ordering) | 1 | Low |
| `StartChat()` | `if (string.IsNullOrEmpty(korisnik1Id))`, `if (korisnik1Id == korisnik2Id)`, `if (chat == null)` | 4 | Low |
| `Poruke()` | `if (chat == null)`, `if (chat.Korisnik1Id != userId && chat.Korisnik2Id != userId)` | 3 | Low |
| `PosaljiPoruku()` | `if (chat == null || ... not in chat)`, `if (string.IsNullOrWhiteSpace(tekst))` | 3 | Low |

**Total ChatController complexity: 11** — **High**

**Recommendation:** The `StartChat` method has complex LINQ for checking bidirectional chat existence. Consider extracting to a service method. Add `[Authorize]` attribute to the class (currently missing per-method).

---

## 8. RecenzijaController.cs (302 lines)

| Method | Decision Points | Complexity | Rating |
|--------|----------------|------------|--------|
| `Index()` | None | 1 | Low |
| `Details()` | `if (id == null)`, `if (recenzija == null)` | 3 | Low |
| `Create()` GET | `if (!User.IsInRole("Admin"))`, `if (!oglasId.HasValue)`, `if (!bypassVerification && (...))` (5 conditions in OR), `if (!User.IsInRole("Admin"))` | 7 | Medium |
| `Create()` POST | `if (!User.IsInRole("Admin"))`, `if (!bypassVerification)`, `if (verifiedOglasId == null || ... )` (4 conditions), `if (recenzija.RadnikId != verifiedRadnikId)`, `else { if (string.IsNullOrEmpty(...)) }`, `if (ModelState.IsValid)`, `try/catch`, `else` | 11 | **High** |
| `Edit()` GET | `if (id == null)`, `if (recenzija == null)` | 3 | Low |
| `Edit()` POST | `if (id != recenzija.Id)`, `if (ModelState.IsValid)`, `try/catch (DbUpdateConcurrencyException)`, `if (!Any)` | 5 | Low |
| `Delete()` GET | `if (id == null)`, `if (recenzija == null)` | 3 | Low |
| `DeleteConfirmed()` | `if (recenzija != null)` | 2 | Low |
| `MojeRecenzije()` | None (LINQ) | 1 | Low |

**Total RecenzijaController complexity: ~36** — **Very High**

**Recommendation:** The `Create()` POST method (CC=11) is the highest-complexity single method in the project. The payment verification logic with session checks, bypass flags, and admin routing should be extracted to a `ReviewAuthorizationService`. The `bypassVerification = false` flag is dead code that adds confusion.

---

## 9. ObavijestKorisnikuController.cs (275 lines)

| Method | Decision Points | Complexity | Rating |
|--------|----------------|------------|--------|
| `Index()` | None | 1 | Low |
| `MyNotifications()` | None (LINQ filter) | 1 | Low |
| `MarkAsRead()` | `if (notification != null)` | 2 | Low |
| `MarkAllAsRead()` | None (LINQ + foreach) | 1 | Low |
| `ClearNotification()` | `if (notification != null)` | 2 | Low |
| `ClearAllNotifications()` | None | 1 | Low |
| `Details()` | `if (id == null)`, `if (obavijestKorisniku == null)` | 3 | Low |
| `Create()` POST | `if (ModelState.IsValid)` | 2 | Low |
| `Edit()` GET | `if (id == null)`, `if (obavijestKorisniku == null)` | 3 | Low |
| `Edit()` POST | `if (id != ...)`, `if (ModelState.IsValid)`, `try/catch (DbUpdateConcurrencyException)`, `if (!Exists)` | 5 | Low |
| `Delete()` GET | `if (id == null)`, `if (obavijestKorisniku == null)` | 3 | Low |
| `DeleteConfirmed()` | `if (obavijestKorisniku != null)` | 2 | Low |
| `MarkAsReadAjax()` | `if (notification != null)` | 2 | Low |

**Total ObavijestKorisnikuController complexity: ~28** — **Medium**

Standard CRUD pattern. Well-structured with consistent authorization attributes.

---

## Top 5 Most Complex Methods (Refactoring Priority)

| Rank | File | Method | CC | Recommendation |
|------|------|--------|-----|----------------|
| 1 | OglasController.cs | `PrikazOglasa()` | 10 | Extract filter/sort to `OglasQueryService` |
| 2 | RecenzijaController.cs | `Create()` POST | 11 | Extract payment verification to `ReviewAuthorizationService` |
| 3 | Program.cs | `CreateAdminUser()` | 8 | Move to `SeedService` class |
| 4 | OglasController.cs | `Edit()` POST | 8 | Extract concurrency + auth to service |
| 5 | ApplicationDbContext.cs | `HandleStripePaymentEventAsync()` | 6 | Extract to `PaymentWebhookHandler` service |

---

## Overall Complexity Summary

| File | Total CC | Rating |
|------|---------|--------|
| OglasController.cs | 69 | Very High |
| Program.cs | 23 | High |
| RecenzijaController.cs | 36 | Very High |
| ObavijestKorisnikuController.cs | 28 | Medium |
| ChatController.cs | 11 | High |
| ApplicationDbContext.cs | 8 | Medium |
| StripeService.cs | 7 | Medium |
| StatisticsService.cs | 5 | Low |
| PaymentTransactionService.cs | 4 | Low |
