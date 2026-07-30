# Continuous Improvement — Iteration 4

## Findings Table

| # | Domain | Tests Added | Score | Status |
|---|--------|-------------|-------|--------|
| 1 | Maintainability (OglasService) | 13 | 7.0 | DONE |
| 2 | Security (anti-forgery) | 6 | 6.0 | DONE |

## Changes Made

### 1. OglasService Unit Tests (Score 7.0)
- Added 13 unit tests for OglasService covering all CRUD operations
- Tests GetOglasByIdAsync (exists/not exists), CreateOglasAsync (field assignment), UpdateOglasAsync (updates/not exists), DeleteOglasAsync (removes/not exists), OglasExistsAsync, GetPrijavljeniOglasiAsync, and GetApplicantsForOglasAsync
- Uses InMemory database for isolation
- Files: OglasServiceTests.cs (Unit/)

### 2. Anti-Forgery Verification Tests (Score 6.0)
- Added 6 anti-forgery integration tests (via parametrized test across 8 endpoints)
- Verifies all POST mutating endpoints reject requests without antiforgery tokens
- Covers Oglas, Recenzija, and Chat controllers
- Files: AntiForgeryTests.cs (Integration/)

## Tests Added
- **OglasService unit tests**: 13
- **Anti-forgery verification tests**: 6
- **Total new tests this iteration**: 19

## Verification Status
- All 13 OglasService unit tests pass
- All 6 anti-forgery verification tests pass
- No regressions in existing tests

## Remaining Risks
| Risk | Score | Plan |
|------|-------|------|
| CI security scanning | 3.0 | Add dotnet list package --vulnerable to CI |
| CI operational readiness | 3.0 | Add vulnerability check step |

## Decision: CONTINUE
- 19 tests added across Maintainability and Security domains
- No regressions
- Only CI-level improvements remain (low risk, high operational value)
