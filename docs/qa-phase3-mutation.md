# Phase-3 Mutation Quality Report

## Approach
Manual mutation analysis (Stryker.NET not available without Docker). Each source method was systematically mutated by hand, and the existing test suite was checked to determine whether each mutant would survive or be killed. New targeted tests were written for every surviving mutant.

## Baseline
- Total tests before phase: 117
- Total tests after phase: 156 (39 new tests added)
- Modules analyzed: StripeService, StatisticsService, PaymentTransactionService, OglasService, ApplicationDbContext.HandleStripePaymentEventAsync

## Survived Mutants Found

| # | Module | Mutation | Risk | Test Added |
|---|--------|----------|------|------------|
| 1 | PaymentTransactionService | `&&` to `\|\|` in IsPaidAsync filter conditions | **HIGH** — any paid transaction incorrectly matches all users | `IsPaid_WithPaidTransactionForDifferentUser_ReturnsFalse`, `IsPaid_WithPaidTransactionForDifferentOglas_ReturnsFalse` |
| 2 | PaymentTransactionService | Change `PaymentStatus.Paid` to `PaymentStatus.Refunded` in IsPaidAsync | MEDIUM — wrong status accepted as "paid" | `IsPaid_ReturnsFalse_WhenStatusIsRefunded` |
| 3 | PaymentTransactionService | Remove `OrderByDescending` from GetByOglasIdAsync | LOW — wrong display order | `GetByOglasIdAsync_ReturnsOrderedDescendingByCreatedAt` |
| 4 | StripeService | `IsNullOrWhiteSpace` to `IsNullOrEmpty` in IsConfigured | MEDIUM — whitespace-only keys treated as valid | `IsConfigured_ReturnsFalse_WhenApiKeyIsWhitespace`, `IsConfigured_ReturnsFalse_WhenApiKeyIsTab` |
| 5 | StripeService | Remove `??` fallback in key resolution | MEDIUM — section fallback never reached | `IsConfigured_PrefersDirectKeyOverSection`, `IsConfigured_FallsBackToSection_WhenDirectKeyIsNull` |
| 6 | StatisticsService | Swap `Contains("Klijent")` with `Contains("Radnik")` | MEDIUM — client/worker counts swapped | `Statistics_RoleCounting_DistinguishesKlijentFromRadnik` |
| 7 | StatisticsService | Remove `AnyAsync()` ternary guard | HIGH — crashes on empty reviews | `Statistics_NoReviews_ReturnsZeroNotException` |
| 8 | StatisticsService | Change `Math.Round(x, 1)` to `Math.Round(x, 0)` | LOW — rounding precision lost | `Statistics_NonStandardDecimalAverage_RoundsToOneDecimal` |
| 9 | OglasService | `o.Status == Status.Aktivan` changed in SearchOglasiAsync base filter | HIGH — inactive jobs shown in search | `SearchOglasiAsync_ExcludesInactiveOglasi` |
| 10 | OglasService | `o.RadnikId == null` changed in SearchOglasiAsync base filter | MEDIUM — taken jobs shown as available | `SearchOglasiAsync_ExcludesOglasiWithRadnikAssigned` |
| 11 | OglasService | `>=` to `>` in minCijena filter (boundary) | MEDIUM — exact minimum price excluded | `SearchOglasiAsync_MinPrice_Inclusive` |
| 12 | OglasService | `<=` to `<` in maxCijena filter (boundary) | MEDIUM — exact maximum price excluded | `SearchOglasiAsync_MaxPrice_Inclusive` |
| 13 | OglasService | `oglas.Status != Status.Aktivan` changed to `==` in ApplyToOglasAsync | HIGH — inactive oglas can be applied to | `ApplyToOglasAsync_ReturnsFalse_WhenOglasIsInactive` |
| 14 | OglasService | `oglas.RadnikId != null` changed to `==` in ApplyToOglasAsync | MEDIUM — taken jobs accept new applicants | `ApplyToOglasAsync_ReturnsFalse_WhenRadnikAlreadyAssigned` |
| 15 | OglasService | Duplicate application check removed | MEDIUM — same user can apply twice | `ApplyToOglasAsync_ReturnsFalse_WhenDuplicateApplication` |
| 16 | OglasService | `Status.Prihvacen` changed in AcceptApplicationAsync | MEDIUM — wrong status on acceptance | `AcceptApplicationAsync_SetsStatusToPrihvacen` |
| 17 | OglasService | `prijava.Oglas.KlijentId != oglasOwnerId` changed to `==` in RejectApplicationAsync | HIGH — owner check inverted | `RejectApplicationAsync_ReturnsFalse_WhenNotOwner` |
| 18 | OglasService | `Status.Odbijen` changed in RejectApplicationAsync | MEDIUM — wrong status on rejection | `RejectApplicationAsync_SetsStatusToOdbijen_WhenOwner` |
| 19 | OglasService | No service-level test for DeleteOglasAsync | MEDIUM — guard clause untested | `DeleteOglasAsync_ReturnsFalse_WhenNotExists`, `DeleteOglasAsync_ReturnsTrue_WhenExists` |
| 20 | OglasService | Search sort switch default/Naslov path | LOW — default sort untested | `SearchOglasiAsync_SortByPriceAsc`, `SearchOglasiAsync_SortByPriceDesc` |
| 21 | OglasService | Search lokacija filter removed | LOW — location filter untested | `SearchOglasiAsync_LokacijaFilter` |
| 22 | OglasService | Search tipPosla filter removed | LOW — job type filter untested | `SearchOglasiAsync_TipPoslaFilter` |
| 23 | OglasService | Search search text filter changed `\|\|` to `&&` | MEDIUM — search only matches title or description, not both | `SearchOglasiAsync_SearchFiltersOnNaslovAndOpis` |
| 24 | OglasService | CreateOglasAsync Status.Aktivan mutation | MEDIUM — new oglas created as inactive | `CreateOglasAsync_SetsStatusToAktivan_AndClearsRadnik` |
| 25 | ApplicationDbContext | Remove PaidAt assignment in new transaction path | MEDIUM — paid timestamp lost | `HandleStripePaymentEvent_NewPaidTransaction_SetsPaidAt` |
| 26 | ApplicationDbContext | Remove PaidAt ternary (always set) | MEDIUM — failed transactions get PaidAt | `HandleStripePaymentEvent_NewFailedTransaction_PaidAtIsNull` |
| 27 | ApplicationDbContext | Remove PaidAt update in else branch | MEDIUM — payment-to-paid transition unrecorded | `HandleStripePaymentEvent_UpdateToPaid_SetsPaidAt` |
| 28 | ApplicationDbContext | Change idempotency key from StripeEventId to PaymentIntentId | HIGH — duplicate events not filtered | `HandleStripePaymentEvent_Idempotency_UsesStripeEventId` |

## Tests Added

| Test Name | Kills Mutation | File |
|-----------|---------------|------|
| Statistics_RoleCounting_DistinguishesKlijentFromRadnik | Swap client/worker counts | Unit/MutationKillTests.cs |
| Statistics_SingleReview_ReturnsExactRating | AnyAsync ternary removal | Unit/MutationKillTests.cs |
| Statistics_NonStandardDecimalAverage_RoundsToOneDecimal | Round precision change | Unit/MutationKillTests.cs |
| Statistics_NoReviews_ReturnsZeroNotException | Remove ternary guard | Unit/MutationKillTests.cs |
| Statistics_FinishedJobs_CountsNeaktivanNotAktivan | Status swap in CountAsync | Unit/MutationKillTests.cs |
| IsPaid_WithPaidTransactionForDifferentUser_ReturnsFalse | && to \|\| in filter | Unit/MutationKillTests.cs |
| IsPaid_WithPaidTransactionForDifferentOglas_ReturnsFalse | && to \|\| in filter | Unit/MutationKillTests.cs |
| IsPaid_ReturnsFalse_WhenStatusIsRefunded | PaidStatus mutation | Unit/MutationKillTests.cs |
| GetByOglasIdAsync_ReturnsOnlyTransactionsForRequestedOglas | Filter inversion | Unit/MutationKillTests.cs |
| GetByOglasIdAsync_ReturnsOrderedDescendingByCreatedAt | Ordering removal | Unit/MutationKillTests.cs |
| GetByUserIdAsync_ReturnsOnlyTransactionsForRequestedUser | Filter inversion | Unit/MutationKillTests.cs |
| IsConfigured_ReturnsFalse_WhenApiKeyIsWhitespace | IsNullOrWhiteSpace → IsNullOrEmpty | Unit/MutationKillTests.cs |
| IsConfigured_ReturnsFalse_WhenApiKeyIsTab | IsNullOrWhiteSpace → IsNullOrEmpty | Unit/MutationKillTests.cs |
| IsConfigured_PrefersDirectKeyOverSection | Null-coalescing mutation | Unit/MutationKillTests.cs |
| IsConfigured_FallsBackToSection_WhenDirectKeyIsNull | Fallback removal | Unit/MutationKillTests.cs |
| CreateOglasAsync_SetsStatusToAktivan_AndClearsRadnik | Status/RadnikId mutation | Unit/MutationKillTests.cs |
| DeleteOglasAsync_ReturnsFalse_WhenNotExists | Guard clause bypass | Unit/MutationKillTests.cs |
| DeleteOglasAsync_ReturnsTrue_WhenExists | Return value mutation | Unit/MutationKillTests.cs |
| ApplyToOglasAsync_ReturnsFalse_WhenOglasIsInactive | Status check mutation | Unit/MutationKillTests.cs |
| ApplyToOglasAsync_ReturnsFalse_WhenRadnikAlreadyAssigned | Null check inversion | Unit/MutationKillTests.cs |
| ApplyToOglasAsync_ReturnsFalse_WhenDuplicateApplication | Duplicate guard removal | Unit/MutationKillTests.cs |
| AcceptApplicationAsync_SetsStatusToPrihvacen | Status assignment mutation | Unit/MutationKillTests.cs |
| RejectApplicationAsync_ReturnsFalse_WhenNotOwner | Ownership check inversion | Unit/MutationKillTests.cs |
| RejectApplicationAsync_SetsStatusToOdbijen_WhenOwner | Status/notification mutation | Unit/MutationKillTests.cs |
| SearchOglasiAsync_ExcludesInactiveOglasi | Base filter mutation | Unit/MutationKillTests.cs |
| SearchOglasiAsync_ExcludesOglasiWithRadnikAssigned | RadnikId filter mutation | Unit/MutationKillTests.cs |
| SearchOglasiAsync_MinPrice_Inclusive | >= to > boundary mutation | Unit/MutationKillTests.cs |
| SearchOglasiAsync_MaxPrice_Inclusive | <= to < boundary mutation | Unit/MutationKillTests.cs |
| SearchOglasiAsync_PriceRange_BothFilters | Compound filter mutation | Unit/MutationKillTests.cs |
| SearchOglasiAsync_SortByPriceAsc | Sort path mutation | Unit/MutationKillTests.cs |
| SearchOglasiAsync_SortByPriceDesc | Sort path mutation | Unit/MutationKillTests.cs |
| SearchOglasiAsync_SearchFiltersOnNaslovAndOpis | Search filter mutation | Unit/MutationKillTests.cs |
| SearchOglasiAsync_LokacijaFilter | Location filter mutation | Unit/MutationKillTests.cs |
| SearchOglasiAsync_TipPoslaFilter | TipPosla filter mutation | Unit/MutationKillTests.cs |
| HandleStripePaymentEvent_NewPaidTransaction_SetsPaidAt | PaidAt assignment removal | Unit/MutationKillTests.cs |
| HandleStripePaymentEvent_NewFailedTransaction_PaidAtIsNull | PaidAt ternary mutation | Unit/MutationKillTests.cs |
| HandleStripePaymentEvent_UpdateToPaid_SetsPaidAt | PaidAt update removal | Unit/MutationKillTests.cs |
| HandleStripePaymentEvent_UpdateToFailed_DoesNotSetPaidAt | PaidAt always-set mutation | Unit/MutationKillTests.cs |
| HandleStripePaymentEvent_Idempotency_UsesStripeEventId | Idempotency key swap | Unit/MutationKillTests.cs |

## Mutation Score Estimate
- **Before**: ~55% (estimated — 28 identifiable survived mutants across 5 modules)
- **After**: ~80% (after targeted additions; all 28 mutants now have at least one killing test)

## Remaining Gaps
- **Controller actions** still have low mutation coverage — mutation analysis of Razor Pages code-behind and controller logic is not covered by this phase
- **EmailService / BrevoEmailSender** — external email integrations have no unit tests; mutations in email sending logic are untested
- **OglasService.GetApplicantsForOglasAsync** — the `requestUserId` parameter is unused in the method body (potential dead code / design issue), no test validates access control on this endpoint
- **OglasService.AcceptApplicationAsync** — does not assign `RadnikId` to the oglas after acceptance (potential logic gap, not a mutation but a correctness issue)
- **Integration test auth flows** — authenticated user flows (CRUD as logged-in user) are not covered, leaving controller mutation score low
