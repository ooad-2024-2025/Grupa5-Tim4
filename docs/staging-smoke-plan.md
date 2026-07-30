# Staging Smoke Test Plan

## Cilj

Verifikovati da je aplikacija funkcionalna nakon deploya u staging okruženje.

## Preduvjeti

- Aplikacija deployovana na staging URL
- PostgreSQL baza dostupna
- Test korisnici seed-ovani (radnik@mail.com, klijent@mail.com)

## Smoke testovi

### 1. Health Checks (automatski)
```bash
curl -f https://staging.naposo.ba/health/live
curl -f https://staging.naposo.ba/health/ready
```
**Očekivano:** Oba vraćaju 200 OK

### 2. Home Page
```bash
curl -f https://staging.naposo.ba/
```
**Očekivano:** 200 OK, sadrži "NaPoso"

### 3. Login Page
```bash
curl -f https://staging.naposo.ba/Identity/Account/Login
```
**Očekivano:** 200 OK, sadrži login formu

### 4. Authentication Flow
```bash
# Login sa test korisnikom
curl -c cookies.txt -d "Input.Email=klijent@mail.com&Input.Password=Test123!" \
  https://staging.naposo.ba/Identity/Account/Login
```
**Očekivano:** Redirect na home page, session cookie postavljen

### 5. API Versioning
```bash
curl -I https://staging.naposo.ba/
```
**Očekivano:** Response headers sadrže `api-supported-versions: 1.0`

### 6. Prometheus Metrics
```bash
curl -f https://staging.naposo.ba/metrics
```
**Očekivano:** 200 OK, Prometheus format podataka

### 7. Stripe Webhook (opcionalno)
```bash
curl -X POST https://staging.naposo.ba/webhook/stripe \
  -H "Content-Type: application/json" \
  -d '{"type":"payment_intent.succeeded"}'
```
**Očekivano:** 400 (invalid signature) — potvrđuje da endpoint postoji

## Vremenski okvir

| Test | Trajanje | Prioritet |
|------|----------|-----------|
| Health checks | 10s | CRITICAL |
| Home page | 5s | CRITICAL |
| Login page | 5s | CRITICAL |
| Auth flow | 30s | CRITICAL |
| API versioning | 5s | HIGH |
| Metrics | 5s | MEDIUM |
| Stripe webhook | 10s | LOW |

**Ukupno:** ~2 minuta za sve smoke testove

## Odobrenje

Svi CRITICAL testovi moraju proći prije nego što se staging označi kao spreman za produkciju.
