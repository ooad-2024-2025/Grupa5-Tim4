# Phase-7 Observability

## Structured Logging
- Replaced Console.WriteLine with ILogger<T> in Program.cs
- Correlation ID middleware enriches all log entries
- Log levels: Information (seeds), Error (failures)

## OpenTelemetry Tracing
- ASP.NET Core instrumentation (request/response spans)
- HTTP client instrumentation (outgoing calls)
- Resource tagged as "NaPoso"

## Prometheus Metrics
- /metrics endpoint exposed for Prometheus scraping
- ASP.NET Core request metrics (count, duration)
- HTTP client call metrics

## Architecture
- Middleware/CorrelationIdMiddleware.cs — request tracing
- Program.cs — logging, tracing, metrics registration
