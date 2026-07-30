# Release Gates and Rollback Procedures

## Go/No-Go Release Checklist

### Pre-Deploy Gates (ALL must pass)
- [ ] All tests pass (211/211)
- [ ] Build succeeds with 0 errors
- [ ] Coverage >= 35%
- [ ] No CRITICAL/HIGH security findings
- [ ] Health checks respond on /health/live and /health/ready
- [ ] Database migrations applied (if any)
- [ ] Configuration secrets verified in environment

### Deploy Gates
- [ ] Docker image builds successfully
- [ ] Container starts and passes health check
- [ ] Smoke test: home page returns 200
- [ ] Smoke test: login page returns 200
- [ ] Smoke test: /health/live returns 200

### Post-Deploy Verification
- [ ] Application accessible on target URL
- [ ] Authentication flow works (login/logout)
- [ ] No 5xx errors in first 5 minutes
- [ ] Prometheus metrics endpoint accessible
- [ ] Logs flowing to configured sink

## Rollback Procedure

### Automated Rollback (Docker)
```bash
# Rollback to previous image
docker-compose down
docker-compose up -d --force-recreate app

# Or rollback to specific version
docker tag grupa5-tim4-app:previous grupa5-tim4-app:latest
docker-compose up -d
```

### Manual Rollback Steps
1. Identify the last known good deployment tag/commit
2. Stop current containers: `docker-compose down`
3. Checkout good version: `git checkout <good-tag>`
4. Rebuild: `docker-compose build`
5. Deploy: `docker-compose up -d`
6. Verify: health checks pass, smoke tests pass

### Rollback Triggers
- Error rate > 5% for 2+ minutes
- Health check failures for 1+ minute
- Authentication flow broken
- Database connection failures

## Environment Configuration
| Environment | URL | DB | Logging |
|------------|-----|-----|----------|
| Development | localhost:5000 | Local PostgreSQL | Console (verbose) |
| Staging | staging.naposo.ba | Staging PostgreSQL | Console (info+) |
| Production | naposo.ba | Production PostgreSQL | Console (warning+) + external |
