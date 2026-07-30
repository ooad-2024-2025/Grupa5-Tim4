# Load Test Baseline

## Prerequisites
- k6 installed: https://k6.io/download/
- Application running on localhost:5000

## Run
```bash
k6 run loadtest/baseline.js
```

## With custom target
```bash
BASE_URL=http://staging:5000 k6 run loadtest/baseline.js
```

## Thresholds
- p95 response time < 500ms
- Error rate < 10%

## Stages
1. Ramp up to 10 VUs (30s)
2. Steady state at 10 VUs (1m)
3. Peak at 20 VUs (30s)
4. Sustained peak (1m)
5. Ramp down (30s)
