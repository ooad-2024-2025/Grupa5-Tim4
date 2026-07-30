# Phase-6 Observability

## Implemented
- **Correlation ID**: X-Correlation-ID header propagated through request/response
- **Global Exception Handler**: Catches unhandled exceptions, logs with correlation ID, returns structured JSON 500
- **Health Endpoints**: /health/live (liveness), /health/ready (readiness with DB check)

## Architecture
- `Middleware/CorrelationIdMiddleware.cs` — generates/propagates correlation IDs
- `Middleware/GlobalExceptionMiddleware.cs` — catches exceptions, logs, returns JSON
- Program.cs — health check registration and endpoint mapping

## Future Enhancements
- Serilog for structured logging
- OpenTelemetry for distributed tracing
- Prometheus metrics endpoint
