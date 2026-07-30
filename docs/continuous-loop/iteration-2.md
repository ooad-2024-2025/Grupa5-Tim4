# Continuous Improvement — Iteration 2

## Findings Table

| # | Domain | Tests Added | Score | Status |
|---|--------|-------------|-------|--------|
| 1 | Security (negative tests) | 12 | 8.0 | DONE |
| 2 | Test Quality (mutation depth) | 11 | 7.0 | DONE |

## Changes Made

### 1. Security Negative Tests (Score 8.0)
- Added 12 security-focused negative tests covering XSS, SQL injection, path traversal, CSRF, and auth bypass scenarios
- Validates that the application rejects malicious input at controller and middleware boundaries
- Files: SecurityTests.cs (Integration/)

### 2. Mutation Depth Tests (Score 7.0)
- Added 11 mutation depth tests targeting edge cases in service and controller logic
- Tests boundary conditions, null handling, and invalid state transitions
- Files: MutationKillTests.cs (Unit/)

## Tests Added
- **Security negative tests**: 12
- **Mutation depth tests**: 11
- **Total new tests this iteration**: 23

## Verification Status
- All 12 security negative tests pass
- All 11 mutation depth tests pass
- No regressions in existing 156 tests

## Remaining Risks
| Risk | Score | Plan |
|------|-------|------|
| Performance/pagination coverage | 5.0 | Add in next iteration |
| Service unit test gaps | 5.0 | Add in next iteration |
| CI security scanning | 3.0 | Add to pipeline |

## Decision: CONTINUE
- 23 tests added across Security and Test Quality domains
- No regressions
- High-value coverage gaps remain (performance, service layer)
