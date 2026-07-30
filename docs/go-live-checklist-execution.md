# Go-Live Checklist — Executed

## Pre-Deploy Gates (obavezno)

- [x] Svi testovi prolaze (211/211) — **VERIFIED**
- [x] Build uspješan (0 errors) — **VERIFIED**
- [x] Coverage ≥35% — **VERIFIED**
- [x] Nema CRITICAL/HIGH security nalaza — **VERIFIED**
- [x] Health check endpointi rade (/health/live, /health/ready) — **VERIFIED**
- [x] Rate limiting konfigurisan (100 req/min) — **VERIFIED**
- [x] Correlation ID middleware aktivan — **VERIFIED**
- [x] Structured logging konfigurisan — **VERIFIED**
- [x] API versioning v1.0 na svim kontrolerima — **VERIFIED**
- [x] OpenTelemetry tracing konfigurisan — **VERIFIED**
- [x] Prometheus metrics endpoint dostupan — **VERIFIED**

## Deploy Gates (obavezno)

- [ ] Docker image gradi se uspješno — **TODO: verifikovati pri deployu**
- [ ] Container startuje i prolazi health check — **TODO: verifikovati pri deployu**
- [ ] Smoke test: home page vraća 200 — **TODO: verifikovati pri deployu**
- [ ] Smoke test: login page vraća 200 — **TODO: verifikovati pri deployu**
- [ ] Smoke test: /health/live vraća 200 — **TODO: verifikovati pri deployu**

## Post-Deploy Verification (obavezno)

- [ ] Aplikacija dostupna na target URL — **TODO: verifikovati**
- [ ] Autentikacija radi (login/logout) — **TODO: verifikovati**
- [ ] Nema 5xx grešaka u prvih 5 minuta — **TODO: verifikovati**
- [ ] Prometheus metrics endpoint dostupan — **TODO: verifikovati**
- [ ] Logovi teku u konfigurisani sink — **TODO: verifikovati**

## Recommended (preporučeno)

- [ ] k6 load test p95<500ms na baseline trafficu — **RECOMMENDED**
- [ ] Grafana dashboard konfigurisan — **RECOMMENDED**
- [ ] Jaeger distributed tracing aktivan — **RECOMMENDED**
- [ ] Serilog sa external sink (Seq/ELK) — **RECOMMENDED**

## Optional (opciono)

- [ ] BenchmarkDotNet regression baseline — **OPTIONAL**
- [ ] Stryker mutation testing u CI — **OPTIONAL (non-blocking)**
- [ ] Load test u CI pipeline — **OPTIONAL**

---

*Checklist ejecutiran: 2026-07-22*
*Verifikovano: 11/11 pre-deploy gates | 0/5 deploy gates (pending deploy) | 0/5 post-deploy (pending deploy)*
