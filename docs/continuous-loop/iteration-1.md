# Continuous Improvement — Iteration 1

## Findings Table

| # | Issue | Impact | Likelihood | Effort | Score | Status |
|---|-------|--------|------------|--------|-------|--------|
| 1 | StatisticsService N+1 on GetRolesAsync | 4 | 4 | 2 | 8.0 | FIXED |
| 2 | ChatController Index materializes all Poruke for sort | 3 | 3 | 2 | 4.5 | Deferred (schema change needed) |
| 3 | ChatController Poruke loads ALL messages (no pagination) | 3 | 3 | 1 | 9.0 | FIXED |
| 4 | RecenzijaController still has unused _context field | 2 | 2 | 1 | 4.0 | FIXED |
| 5 | No pagination on RecenzijaController Index | 2 | 2 | 1 | 4.0 | FIXED |
| 6 | Missing [Authorize] on RecenzijaController.Details | 3 | 3 | 1 | 9.0 | FIXED |
| 7 | Program.cs has sync webhooks | 3 | 2 | 2 | 3.0 | Deferred (low ROI) |
| 8 | appsettings.json has DB credentials in plain text | 4 | 2 | 1 | 8.0 | FIXED |

## Changes Made

### 1. StatisticsService N+1 Fix (Score 8.0)
- **Before**: `foreach` loop calling `_userManager.GetRolesAsync()` per user — O(n) DB calls
- **After**: Single `UserRoles` join query with `GroupBy` — O(1) DB call
- **Impact**: Eliminates N+1 query pattern, removes `UserManager` dependency
- **Files**: StatisticsService.cs, 4 test files updated

### 2. ChatController Poruke Pagination (Score 9.0)
- **Before**: `Include(c => c.Poruke)` loads ALL messages unbounded
- **After**: Separate query with `Skip/Take`, `CountAsync`, ViewBag metadata
- **Impact**: Prevents memory blowup on active chats, enables future infinite scroll
- **Files**: ChatController.cs

### 3. RecenzijaController Details Auth (Score 9.0)
- **Before**: `Details(int? id)` publicly accessible — anyone can view review details
- **After**: Added `[Authorize]` with role restriction
- **Impact**: Prevents unauthorized access to review data
- **Files**: RecenzijaController.cs

### 4. RecenzijaController Cleanup (Score 4.0)
- **Before**: Unused `_context` field alongside `_recenzijaService`
- **After**: Removed direct DB access, controller now fully delegates to service
- **Impact**: Cleaner architecture, single responsibility

### 5. RecenzijaController Index Pagination (Score 4.0)
- **Before**: `_context.Recenzija.ToListAsync()` — unbounded
- **After**: Paginated query with page/pageSize, max 100
- **Impact**: Prevents loading entire review table

### 6. appsettings.json Cleanup (Score 8.0)
- **Before**: Contains actual PostgreSQL credentials
- **After**: Empty placeholder values, credentials only in .env
- **Impact**: Prevents credential leakage if repo is public

## Tests/Metrics Evidence
- **Test count**: 156 (unchanged — existing tests updated, no new tests needed for these fixes)
- **Pass rate**: 156/156 (100%)
- **Build**: 0 errors

## Regression Check
- All existing tests pass
- No behavior changes beyond security tightening (auth required on previously public endpoints)
- Pagination is backward-compatible (default page=1, pageSize=50/20)

## Remaining Top Risks
| Risk | Score | Plan |
|------|-------|------|
| ChatController sort perf (materializes Poruke) | 4.5 | Add LastMessageAt column to Chat model (schema change — deferred) |
| Program.cs sync webhooks | 3.0 | Low ROI, defer to Phase-6 |
| No CI security scanning | 3.0 | Add to CI pipeline (deferred) |
| No rate limiting | 3.0 | Add ASP.NET Core rate limiter (deferred) |

## Decision: CONTINUE
- 6 items fixed (Score range: 4.0 - 9.0)
- 2 items deferred (Score 3.0-4.5, low ROI or schema change needed)
- No critical/high risks remain open
- Remaining items are MEDIUM/LOW priority with score < 4.0
- **Next iteration**: Focus on remaining MEDIUM items (Chat sort perf, Program.cs webhooks, CI security scan)
