# NaPoso — Oglasi za poslove

Web aplikacija za povezivanje klijenata i radnika kroz sistematic oglašavanja poslova, sa ugrađenim plaćanjem, notifikacijama i verifikacijom korisnika.

## Tech Stack

- **.NET 8.0** (ASP.NET Core MVC + Razor Pages)
- **PostgreSQL** (via EF Core + Npgsql)
- **ASP.NET Core Identity** (autentikacija/autorizacija)
- **Stripe** (plaćanje)
- **Bootstrap 5** + custom CSS
- **Docker** (lokalno pokretanje)

## Brzi početak

### 1. Lokalno sa Docker Compose (preporučeno)

```bash
# Kopiraj env fajl
cp .env.example .env

# Pokreni sve servise
docker compose up --build
```

Aplikacija će biti dostupna na `http://localhost:5000`.

### 2. Lokalno bez Docker-a

Zahtjevi:
- .NET 8.0 SDK
- PostgreSQL (lokalno ili u Docker-u)

```bash
# Kopiraj env fajl
cp .env.example .env

# Pokreni PostgreSQL (ili koristi docker za samo bazu)
docker run -d --name naposo-db -p 5432:5432 \
  -e POSTGRES_DB=naposo \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  postgres:16-alpine

# Instaliraj dependencies
dotnet restore NaPoso/NaPoso.sln

# Pokreni migracije i aplikaciju
dotnet run --project NaPoso/NaPoso
```

### 3. User Secrets (lokalni razvoj)

```bash
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project NaPoso/NaPoso
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..." --project NaPoso/NaPoso
dotnet user-secrets set "Email:Brevo:ApiKey" "your-key" --project NaPoso/NaPoso
```

### 4. Reset baze (obriši sve i kreiraj iznova)

Aplikacija koristi `EnsureCreated` + seed pri startu. Da bi se sve ponovo kreiralo, baza mora biti potpuno obrisana.

**Docker Compose (preporučeno):**

```powershell
# Brise volume sa podacima i pokrece sve iznova
.\scripts\reset-database.ps1 -UseDocker
```

Ili ručno:

```bash
docker compose down -v
docker compose up --build
```

**Lokalni PostgreSQL (bez Docker-a):**

```powershell
.\scripts\reset-database.ps1
dotnet run --project NaPoso/NaPoso
```

Ili ručno preko psql:

```bash
psql -U postgres -c "DROP DATABASE IF EXISTS naposo WITH (FORCE);"
psql -U postgres -c "CREATE DATABASE naposo;"
dotnet run --project NaPoso/NaPoso
```

Nakon reseta, seed kreira:
- Admin: `admin@mail.com` / `Admin123!`
- Test klijent: `klijent@mail.com` / `Test123!`
- Test radnik: `radnik@mail.com` / `Test123!`
- + 10 klijenata, 15 radnika, 35 oglasa (dummy podaci)

## Konfiguracija

| Varijable | Opis | Default |
|-----------|------|---------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string | `Host=localhost;...` |
| `Stripe:SecretKey` | Stripe tajni ključ | (prazno) |
| `Stripe:PublishableKey` | Stripe javni ključ | (prazno) |
| `Stripe:WebhookSecret` | Stripe webhook tajna | (prazno) |
| `Email:Provider` | Email provider (`brevo` ili `console`) | `console` |
| `Email:Brevo:ApiKey` | Brevo API ključ | (prazno) |
| `Email:Brevo:BaseUrl` | Brevo API URL | (prazno) |
| `Email:From` | Adresa pošiljaoca | `noreply@naposo.example.com` |
| `Admin:Email` | Admin email za seed | `admin@mail.com` |
| `Admin:Password` | Admin lozinka za seed | `Admin123!` |

## Arhitektura

### Email Provider Pattern

Email slanje koristi `IEmailSender` interface sa dva provider-a:

- **BrevoEmailSender** — HTTP klijent za Brevo API (produkcijski)
- **ConsoleEmailSender** — loguje u konzolu (razvoj/test)

Konfiguracija `Email:Provider` bira aktivni provider.

### Stripe Payment Status

Webhook endpoint na `/webhook/stripe` beleži status plaćanja u `PaymentTransaction` tabelu:

- `Pending` — obrada u toku
- `Paid` — uspešno plaćeno
- `Failed` — neuspešno
- `Refunded` — refundirano

Svaki webhook event je idempotentan (StripeEventId indeks).

### Statistika

Admin panel prikazuje agregirane podatke:
- Ukupan broj korisnika (klijenti + radnici)
- Broj poslova (aktivni, završeni, plaćeni)
- Prosječna ocjena recenzija

## CI/CD

GitHub Actions workflow-ovi:

- **`.github/workflows/ci.yml`** — restore + build + test (sa PostgreSQL service container)
- **`.github/workflows/docker.yml`** — Docker build verifikacija

## TODO za produkciju

- [x] Struktuirani logging (ILogger + Correlation ID)
- [x] Health checks (/health/live, /health/ready)
- [x] Rate limiting (100 req/min)
- [x] Path traversal defense
- [x] Anti-forgery enforcement
- [x] API versioning (v1.0)
- [x] OpenTelemetry tracing
- [x] Prometheus metrics
- [x] CI quality gates (coverage + vulnerability scan)
- [x] k6 load test baseline
- [x] Release gates & rollback procedures
- [ ] Zamijeniti placeholder Brevo API ključeve sa producijskim
- [ ] Konfigurisati Stripe webhook secret za produkciju
- [ ] Deploy na hosting platformu
- [ ] Postaviti HTTPS redirect za produkciju
- [ ] Podesiti CORS za produkciju

## Production Readiness

Aplikacija je prošla kroz 8 faza quality hardeninga:

| Faza | Focus | Testovi |
|------|-------|---------|
| Phase 1-2 | UI/UX, dark mode, CSS, password toggle | 60 → 117 |
| Phase 3 | Mutation testing, security, performance | 117 → 156 |
| Phase 4 | Auth lockdown, path traversal, anti-forgery | 156 → 156 |
| Phase 5 | Health checks, rate limiting, observability | 156 → 202 |
| Phase 6 | Continuous improvement loop (3 iterations) | 202 → 211 |
| Phase 7 | Serilog, OTel, Prometheus, API versioning, benchmarks | 211 |
| Phase 8 | Load testing, release gates, operational docs | 211 |

**Ukupno: 211 testova, 100% pass rate.**

### Operativne kontrole
- ✅ Health checks (/health/live, /health/ready)
- ✅ Rate limiting (100 req/min)
- ✅ Correlation ID middleware
- ✅ Structured logging (ILogger)
- ✅ OpenTelemetry tracing (OTLP)
- ✅ Prometheus metrics (/metrics)
- ✅ API versioning (v1.0)
- ✅ CI quality gates + vulnerability scan
- ✅ k6 load test baseline

### Dokumentacija
- [Go-Live Verdict](docs/go-live-verdict.md)
- [Release Checklist](docs/go-live-checklist-execution.md)
- [Staging Smoke Plan](docs/staging-smoke-plan.md)
- [Rollback Quick Guide](docs/rollback-quick-guide.md)
- [QA Reports](docs/qa-phase*.md)

## Struktura projekta

```
Grupa5-Tim4/
├── NaPoso/
│   ├── NaPoso.sln
│   └── NaPoso/
│       ├── Controllers/       # MVC kontroleri
│       ├── Data/              # DbContext + migracije
│       ├── Enums/             # Enum definicije
│       ├── Models/            # Domain modeli
│       ├── Services/          # Business logika
│       ├── Views/             # Razor views
│       ├── Areas/Identity/    # Identity pages (login, payment)
│       └── wwwroot/           # Statički fajlovi
├── .github/workflows/         # CI/CD
├── .agents/                   # Agent instrukcije
├── Dockerfile
├── docker-compose.yml
├── .env.example
└── .gitignore
```

## Licenca

Projekat za potrebe predmeta OOAD (2024/2025), Grupa 5, Tim 4.
