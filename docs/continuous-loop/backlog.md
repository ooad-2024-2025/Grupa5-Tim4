# Continuous Improvement Backlog (Final — After Iteration 5)

## KPI Status: ALL MET ✅

| KPI | Target | Achieved |
|-----|--------|----------|
| Tests | 181+ | **202** (+46) |
| Coverage | +5pp | ~+15pp |
| Complexity | 2 methods -20% | OglasController -64%, RecenzijaController debug removed |
| Security scenarios | 12 | **12** |
| Perf scenarios | 8 | **11** |
| CI checks | 1 | **1** (vulnerability scan) |

## Completed Items (All Iterations)

### Phase 1-4 (Foundation)
| # | Item | Status |
|---|------|--------|
| 1 | Dark mode + password SVG | ✅ |
| 2 | CSS modularization | ✅ |
| 3 | ChatController auth lockdown | ✅ |
| 4 | Path traversal defense | ✅ |
| 5 | Anti-forgery enforcement | ✅ |
| 6 | RoleConstants | ✅ |
| 7 | RecenzijaService extraction | ✅ |
| 8 | Pagination on OglasService | ✅ |
| 9 | Secrets hygiene | ✅ |
| 10 | CI quality gates | ✅ |

### Continuous Loop Iterations 1-5
| # | Item | Iteration | Status |
|---|------|-----------|--------|
| 11 | StatisticsService N+1 fix | 1 | ✅ |
| 12 | ChatController Poruke pagination | 1 | ✅ |
| 13 | RecenzijaController Details auth | 1 | ✅ |
| 14 | RecenzijaController cleanup | 1 | ✅ |
| 15 | appsettings.json credentials | 1 | ✅ |
| 16 | 12 security negative tests | 2 | ✅ |
| 17 | 11 mutation depth tests | 2 | ✅ |
| 18 | 11 pagination/perf tests | 3 | ✅ |
| 19 | 13 OglasService unit tests | 4 | ✅ |
| 20 | 6 anti-forgery tests | 4 | ✅ |
| 21 | CI vulnerability scan | 5 | ✅ |

## Remaining Optional Items (Future)
| # | Item | Effort | Benefit |
|---|------|--------|---------|
| 1 | ChatController sort perf (schema migration) | High | Medium |
| 2 | Program.cs Stripe webhook refactor | High | Low |
| 3 | Rate limiting | Medium | Medium |
| 4 | Observability (logging/metrics) | Medium | Medium |
| 5 | Health checks | Low | Low |
| 6 | Stryker.NET in CI | Medium | Medium |
| 7 | BenchmarkDotNet | Medium | Low |
