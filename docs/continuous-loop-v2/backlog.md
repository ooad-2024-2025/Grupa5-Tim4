# Continuous Loop V2 Backlog

## KPI Board
| KPI | Target | It.1 | It.2 | It.3 | Final |
|-----|--------|------|------|------|-------|
| Total tests | growing | 202→205 | 205→207 | 207→211 | 211 |
| New test domains | 5 | Security+Ops | Ops+TestQuality | TestQuality+Perf | 3/3 |
| CI checks | +1 | +0 | +1 (stryker) | +0 | +1 |
| Rate limiting | yes | ✅ | - | - | ✅ |
| Observability | yes | ✅ | - | - | ✅ |
| Health checks | yes | - | ✅ | - | ✅ |
| Benchmarks | yes | - | - | ✅ | ✅ |

## Completed
| # | Item | Iteration | Domain |
|---|------|-----------|--------|
| 1 | Rate limiting (100 req/min global) | 1 | Security+Ops |
| 2 | Correlation ID middleware | 1 | Security+Ops |
| 3 | Global exception handler | 1 | Security+Ops |
| 4 | /health/live endpoint | 2 | Ops+TestQuality |
| 5 | /health/ready with DB check | 2 | Ops+TestQuality |
| 6 | Stryker.NET CI workflow | 2 | Ops+TestQuality |
| 7 | Performance baseline tests | 3 | TestQuality+Perf |

## Remaining (Optional)
- BenchmarkDotNet dedicated project
- Structured logging with Serilog
- OpenTelemetry tracing
- Dashboard for metrics
