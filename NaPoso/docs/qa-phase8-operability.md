# Phase-8 Operability

## Structured Logging
- Environment-aware logging: Development (verbose) vs Production (warning+)
- Console sink with timestamps and scopes in production
- Filtered noise from Microsoft.AspNetCore and EF Core in production

## OpenTelemetry
- ASP.NET Core request tracing
- HTTP client call tracing
- OTLP exporter for Jaeger/Zipkin backend
- Prometheus metrics for Grafana dashboards

## Monitoring Stack
- **Jaeger**: Distributed tracing (localhost:16686 UI)
- **Prometheus**: Metrics scraping (localhost:9090)
- **Grafana**: Dashboard visualization (localhost:3000)

## docker-compose.otel.yml
Local development monitoring stack:
- Jaeger all-in-one
- Prometheus with scrape config
- Grafana with admin access

## Alert Rules
- High Error Rate (>5% for 2min) → Critical
- High Latency (p95 > 2s for 5min) → Warning
- Service Down (health check fails for 1min) → Critical
