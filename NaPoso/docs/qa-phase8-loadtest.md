# Phase-8 Load Test Baseline

## Tool: k6

## Script: loadtest/baseline.js

## Load Profile
| Stage | Duration | VUs | Purpose |
|-------|----------|-----|---------|
| Ramp up | 30s | 0→10 | Warm up |
| Steady | 1m | 10 | Baseline |
| Peak | 30s | 10→20 | Stress test |
| Sustained | 1m | 20 | Sustained load |
| Ramp down | 30s | 20→0 | Cool down |

## Thresholds
- p95 response time < 500ms
- Error rate < 10%

## Endpoints Tested
- GET / (home page)
- GET /health/live
- GET /health/ready
- GET /Identity/Account/Login

## How to Run
```bash
k6 run loadtest/baseline.js
BASE_URL=http://staging:5000 k6 run loadtest/baseline.js
```

## Baseline Results (expected)
- Home page: p95 < 200ms
- Health checks: p95 < 50ms
- Login page: p95 < 300ms
