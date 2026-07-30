# Iteration 1 — Security + Operations

## Domains: Security, Operations
## Tests: 202 → 205 (+3)

## Changes
1. Rate limiting: Global 100 req/min fixed window limiter
2. Correlation ID middleware: X-Correlation-ID header propagation
3. Global exception middleware: catches unhandled exceptions, logs with correlation ID

## Verification
- 205/205 tests pass
- CorrelationId propagated in responses
- Rate limiter configured

## Decision: CONTINUE (domains rotated for It.2)
