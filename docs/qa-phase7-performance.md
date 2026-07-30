# Phase-7 Performance

## BenchmarkDotNet Project
- Location: NaPoso.Benchmarks/
- Framework: net8.0
- Package: BenchmarkDotNet 0.14.0

## Baseline Benchmarks
| Benchmark | Params | Purpose |
|-----------|--------|--------|
| StatisticsService.GetStatistics | 10/100/1000 oglasi | Core aggregation perf |

## Run Instructions
```bash
cd NaPoso.Benchmarks
dotnet run -c Release
```

## Expected Baseline (from unit tests)
- 10 oglasi: <100ms
- 100 oglasi: <500ms
- 1000 oglasi: <2000ms
