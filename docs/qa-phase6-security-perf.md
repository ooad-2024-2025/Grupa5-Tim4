# Phase-6 Security + Performance

## Security
- Rate limiting: 100 req/min global fixed window
- Correlation ID for request tracing
- Global exception handler prevents stack trace leakage

## Performance
- Benchmark baseline tests established
- StatisticsService: <5s with 100 oglasi
- OglasService search: <3s with 100 oglasi
- Pagination verified correct

## CI
- Vulnerability scanning (dotnet list package --vulnerable)
- Stryker.NET mutation testing (non-blocking)
