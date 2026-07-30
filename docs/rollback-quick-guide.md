# Rollback Quick Guide

## Brzi rollback (Docker)

```bash
# 1. Zaustavi trenutnu aplikaciju
docker-compose down

# 2. Pokreni prethodnu verziju
docker-compose up -d --force-recreate app

# 3. Verifikuj health check
curl http://localhost:5000/health/live
```

## Rollback na specifičnu verziju

```bash
# 1. Pronađi zadnju dobru oznaku
git log --oneline -10

# 2. Checkout dobre verzije
git checkout <good-tag>

# 3. Rebuild i deploy
docker-compose build
docker-compose up -d

# 4. Verifikuj
curl http://localhost:5000/health/live
curl http://localhost:5000/health/ready
```

## Rollback triggeri

| Uslov | Akcija |
|-------|--------|
| Error rate > 5% za 2+ minuta | Automatski rollback |
| Health check fails 1+ minutu | Automatski rollback |
| Auth flow ne radi | Ručni rollback |
| DB connection failures | Ručni rollback |

## Post-rollback verifikacija

1. Health check: `curl /health/live` → 200
2. Smoke test: `curl /` → 200
3. Login test: `curl /Identity/Account/Login` → 200
4. Metrics: `curl /metrics` → 200

## Kontakt

- **On-call:** ____________
- **Slack channel:** ____________
- **Escalation:** ____________
