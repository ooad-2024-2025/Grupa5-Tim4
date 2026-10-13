# NaPoso — Job Listings Platform

Web app that connects clients with workers through job postings. Has built-in payments (Stripe), notifications, and user verification.

**Live at:** [naposo.me](https://naposo.me)

## Tech Stack

- **.NET 8.0** (ASP.NET Core MVC + Razor Pages)
- **PostgreSQL** (EF Core + Npgsql)
- **ASP.NET Core Identity** (auth)
- **Stripe** (payments)
- **Bootstrap 5** + custom CSS
- **Docker**

## Quick Start

### 1. Docker Compose (recommended)

```bash
cp .env.example .env
docker compose up --build
```

App runs at `http://localhost:5000`.

### 2. Without Docker

Requirements: .NET 8.0 SDK, PostgreSQL

```bash
cp .env.example .env

# Spin up just the database
docker run -d --name naposo-db -p 5432:5432 \
  -e POSTGRES_DB=naposo \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  postgres:16-alpine

dotnet restore NaPoso/NaPoso.sln
dotnet run --project NaPoso/NaPoso
```

### 3. User Secrets (local dev)

```bash
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project NaPoso/NaPoso
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..." --project NaPoso/NaPoso
dotnet user-secrets set "Email:Brevo:ApiKey" "your-key" --project NaPoso/NaPoso
```

### 4. Database Reset

The app uses `EnsureCreated` + seed on startup. To start fresh, nuke the database.

**Docker Compose:**

```bash
docker compose down -v
docker compose up --build
```

**Local PostgreSQL:**

```bash
psql -U postgres -c "DROP DATABASE IF EXISTS naposo WITH (FORCE);"
psql -U postgres -c "CREATE DATABASE naposo;"
dotnet run --project NaPoso/NaPoso
```

After reset, seed creates:
- Admin: `admin@mail.com` / `Admin123!`
- Test client: `klijent@mail.com` / `Test123!`
- Test worker: `radnik@mail.com` / `Test123!`
- 10 clients, 15 workers, 35 job listings (dummy data)

## Configuration

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string | `Host=localhost;...` |
| `Stripe:SecretKey` | Stripe secret key | (empty) |
| `Stripe:PublishableKey` | Stripe publishable key | (empty) |
| `Stripe:WebhookSecret` | Stripe webhook secret | (empty) |
| `Email:Provider` | `brevo` or `console` | `console` |
| `Email:Brevo:ApiKey` | Brevo API key | (empty) |
| `Email:Brevo:BaseUrl` | Brevo API URL | (empty) |
| `Email:From` | Sender address | `noreply@naposo.example.com` |
| `Admin:Email` | Admin email for seed | `admin@mail.com` |
| `Admin:Password` | Admin password for seed | `Admin123!` |

## Architecture

### Email

Uses `IEmailSender` with two providers:
- **BrevoEmailSender** — calls Brevo API over HTTP (production)
- **ConsoleEmailSender** — prints to stdout (dev/test)

Set via `Email:Provider`.

### Payments

Stripe webhook at `/webhook/stripe` tracks payment status in `PaymentTransaction`:
- `Pending` → `Paid` / `Failed` / `Refunded`

Each webhook event is idempotent (StripeEventId unique index).

### Admin Stats

Dashboard shows: total users, job counts (active/completed/paid), average review rating.

## CI/CD

- **`.github/workflows/ci.yml`** — restore + build + test (PostgreSQL service container)
- **`.github/workflows/docker.yml`** — Docker build check

## Production Checklist

Done:
- [x] Structured logging (ILogger + Correlation ID)
- [x] Health checks (`/health/live`, `/health/ready`)
- [x] Rate limiting (100 req/min)
- [x] Path traversal defense
- [x] Anti-forgery enforcement
- [x] API versioning (v1.0)
- [x] OpenTelemetry tracing
- [x] Prometheus metrics
- [x] CI quality gates (coverage + vuln scan)
- [x] k6 load test baseline
- [x] Release gates & rollback procedures

Still needed:
- [ ] Production Brevo API keys
- [ ] Production Stripe webhook secret
- [ ] Deploy to hosting
- [ ] HTTPS redirect
- [ ] CORS config

## Test Coverage

211 tests, all passing. Built up across 8 hardening phases covering UI, security, auth, observability, and load testing.

### Operational Controls

- Health checks (`/health/live`, `/health/ready`)
- Rate limiting (100 req/min)
- Correlation ID middleware
- Structured logging (ILogger)
- OpenTelemetry tracing (OTLP)
- Prometheus metrics (`/metrics`)
- API versioning (v1.0)

### Docs

- [Go-Live Verdict](docs/go-live-verdict.md)
- [Release Checklist](docs/go-live-checklist-execution.md)
- [Staging Smoke Plan](docs/staging-smoke-plan.md)
- [Rollback Quick Guide](docs/rollback-quick-guide.md)
- [QA Reports](docs/qa-phase*.md)

## Project Structure

```
Grupa5-Tim4/
├── NaPoso/                    # Main .NET project
│   ├── NaPoso.sln
│   └── NaPoso/
│       ├── Controllers/       # MVC controllers
│       ├── Data/              # DbContext + migrations
│       ├── Enums/
│       ├── Models/            # Domain models
│       ├── Services/          # Business logic
│       ├── Views/             # Razor views
│       ├── Areas/Identity/    # Identity pages (auth, payment)
│       └── wwwroot/           # Static files (CSS, JS, images)
├── docs/                      # Documentation & test reports
├── scripts/                   # DB & deployment scripts
├── grafana/                   # Grafana dashboard configs
├── loadtest/                  # k6 load testing scripts
├── .github/workflows/         # CI/CD (GitHub Actions)
├── Dockerfile                 # Multi-stage Docker build
├── docker-compose.yml         # App + DB
├── docker-compose.otel.yml    # Telemetry (Prometheus, Grafana, Jaeger)
├── prometheus.yml
├── .env.example
└── .gitignore
```

## License

University project for OOAD course (2024/2025), Group 5, Team 4.
