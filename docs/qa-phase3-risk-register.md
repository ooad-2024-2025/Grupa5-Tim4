# Phase-3 Risk Register

## High Risk
| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Controller layer low coverage | Regression undetected | High | Add more integration tests |
| No mutation testing in CI | Weak tests pass | Medium | Add Stryker.NET in Phase-4 |
| RecenzijaController CC=40 | Hard to maintain | Medium | Refactor to service in Phase-4 |

## Medium Risk
| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| StatisticsService .Result deadlock risk | App crash under load | Low | Already fixed, monitoring needed |
| Magic strings (role names) | Typo causes bug | Medium | Introduce RoleConstants |
| No performance benchmarks | Regressions undetected | Medium | Add BenchmarkDotNet in Phase-4 |

## Low Risk
| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| 58 hardcoded role strings | Maintenance burden | Low | Centralize in constants |
| Unused CSS classes may return | Dead code accumulation | Low | Periodic cleanup |
| Missing CancellationToken propagation | Potential timeout issues | Low | Audit in Phase-4 |
