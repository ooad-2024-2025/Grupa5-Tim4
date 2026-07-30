# Phase-3 Security Hardening Report

## Auth Policy Matrix

| Controller | Action | Auth Required | Roles | Status |
|-----------|--------|---------------|-------|--------|
| **HomeController** | Index | No | — | Public (intentional) |
| HomeController | Admin | Yes | Admin | OK |
| HomeController | Radnik | Yes | Radnik | OK |
| HomeController | Klijent | Yes | Klijent | OK |
| HomeController | Error | No | — | Public (intentional) |
| **OglasController** | Index | Yes | Admin | OK |
| OglasController | Details | Yes | Admin,Klijent,Radnik | OK |
| OglasController | Create (GET) | Yes | Admin,Klijent | OK |
| OglasController | Create (POST) | Yes | Admin,Klijent | OK |
| OglasController | Edit (GET) | Yes | Admin,Klijent | OK |
| OglasController | Edit (POST) | Yes | Admin,Klijent | OK |
| OglasController | Delete (GET) | Yes | Admin,Klijent | OK |
| OglasController | Delete (POST) | Yes | Admin,Klijent | OK |
| OglasController | PrikazOglasa | Yes | Radnik | OK |
| OglasController | OglasiKlijenta | Yes | Admin,Klijent | OK |
| OglasController | PrijavljeniRadnici | Yes | Admin,Klijent | OK |
| OglasController | PrijaviRadnikaNaOglas | Yes | Radnik | OK |
| OglasController | PrijaviSe | Yes | Radnik | OK |
| OglasController | Prihvati | Yes | Admin,Klijent | OK |
| OglasController | Odbij | Yes | Admin,Klijent | OK |
| OglasController | InitiatePayment | Yes | Klijent | OK |
| OglasController | UspjesnaPrijava | No | — | **MISSING** — should require Radnik |
| OglasController | PrijavaGreska | No | — | **MISSING** — should require Radnik |
| OglasController | KreirajPosao (GET) | Yes | Admin | OK |
| OglasController | KreirajPosao (POST) | Yes | Admin | OK |
| **ChatController** | Index | No | — | **MISSING** — chat list exposed to anonymous |
| ChatController | StartChat | No | — | **MISSING** — anyone can create chats |
| ChatController | Poruke | No | — | **MISSING** — message history exposed |
| ChatController | PosaljiPoruku (POST) | No | — | **MISSING** — anyone can send messages |
| **RecenzijaController** | Index | Yes | Admin | OK |
| RecenzijaController | Details | No | — | **MISSING** — review details public |
| RecenzijaController | Create (GET) | Yes | Klijent,Admin | OK |
| RecenzijaController | Create (POST) | Yes | Klijent,Admin | OK |
| RecenzijaController | Edit (GET) | Yes | Admin | OK |
| RecenzijaController | Edit (POST) | Yes | Admin | OK |
| RecenzijaController | Delete (GET) | Yes | Admin | OK |
| RecenzijaController | Delete (POST) | Yes | Admin | OK |
| RecenzijaController | MojeRecenzije | Yes | Radnik | OK |
| **ObavijestController** | Index | Yes | Admin | OK |
| ObavijestController | Details | Yes | Admin | OK |
| ObavijestController | Create (GET) | No | — | **MISSING** — unauthenticated access to create form |
| ObavijestController | Create (POST) | No | — | **MISSING** — anyone can create notifications |
| ObavijestController | Edit (GET) | Yes | Admin | OK |
| ObavijestController | Edit (POST) | Yes | Admin | OK |
| ObavijestController | Delete (GET) | Yes | Admin | OK |
| ObavijestController | Delete (POST) | Yes | Admin | OK |
| **ObavijestKorisnikuController** | Index | Yes | Admin | OK |
| ObavijestKorisnikuController | MyNotifications | Yes | Admin,Klijent,Radnik | OK |
| ObavijestKorisnikuController | MarkAsRead (POST) | Yes | Admin,Klijent,Radnik | OK |
| ObavijestKorisnikuController | MarkAllAsRead (POST) | Yes | Admin,Klijent,Radnik | OK |
| ObavijestKorisnikuController | ClearNotification (POST) | Yes | Admin,Klijent,Radnik | OK |
| ObavijestKorisnikuController | ClearAllNotifications (POST) | Yes | Admin,Klijent,Radnik | OK |
| ObavijestKorisnikuController | Details | Yes | Admin | OK |
| ObavijestKorisnikuController | Create (GET) | Yes | Admin | OK |
| ObavijestKorisnikuController | Create (POST) | Yes | Admin | OK |
| ObavijestKorisnikuController | Edit (GET) | Yes | Admin | OK |
| ObavijestKorisnikuController | Edit (POST) | Yes | Admin | OK |
| ObavijestKorisnikuController | Delete (GET) | Yes | Admin | OK |
| ObavijestKorisnikuController | Delete (POST) | Yes | Admin | OK |
| ObavijestKorisnikuController | MarkAsReadAjax (POST) | Yes | Admin,Klijent,Radnik | OK |
| **AdminController** | Index | Yes | Admin | OK |
| AdminController | Documents | Yes | Admin | OK |
| AdminController | DeleteDocument (POST) | Yes | Admin | OK |
| AdminController | ApproveDocument (POST) | Yes | Admin | OK |
| **OglasKorisnikController** | All actions | Yes | Admin | OK (class-level) |
| **Stripe webhook** | POST /webhook/stripe | No | — | OK (verified via Stripe-Signature header) |

## Antiforgery Audit

| Controller | POST Actions | [ValidateAntiForgeryToken] | Status |
|-----------|-------------|--------------------------|--------|
| **OglasController** | Create | Yes | OK |
| OglasController | Edit | Yes | OK |
| OglasController | Delete | Yes | OK |
| OglasController | KreirajPosao | Yes | OK |
| **ChatController** | PosaljiPoruku | **No** | **MISSING** |
| **RecenzijaController** | Create | Yes | OK |
| RecenzijaController | Edit | Yes | OK |
| RecenzijaController | Delete | Yes | OK |
| **ObavijestController** | Create | Yes | OK |
| ObavijestController | Edit | Yes | OK |
| ObavijestController | Delete | Yes | OK |
| **ObavijestKorisnikuController** | Create | Yes | OK |
| ObavijestKorisnikuController | Edit | Yes | OK |
| ObavijestKorisnikuController | Delete | Yes | OK |
| ObavijestKorisnikuController | MarkAsRead | Yes | OK |
| ObavijestKorisnikuController | MarkAllAsRead | Yes | OK |
| ObavijestKorisnikuController | ClearNotification | Yes | OK |
| ObavijestKorisnikuController | ClearAllNotifications | Yes | OK |
| ObavijestKorisnikuController | MarkAsReadAjax | Yes | OK |
| **OglasKorisnikController** | Create | Yes | OK |
| OglasKorisnikController | Edit | Yes | OK |
| OglasKorisnikController | Delete | Yes | OK |

## Input Validation

| Check | Status | Notes |
|-------|--------|-------|
| SQL injection | **Safe** | All queries use EF Core LINQ; no raw SQL or string concatenation in queries |
| XSS | **Needs review** | Views use `@` (auto-escaped by Razor) — safe by default. Chat messages (`@poruka.Tekst`) and review content (`@recenzija.Sadrzaj`) are properly escaped. No `@Html.Raw()` usage found on user input. |
| Path traversal | **VULNERABLE** | `AdminController.DeleteDocument(string fileName)` uses `Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents", fileName)` without sanitizing `fileName`. An attacker could supply `../../etc/passwd` or `../../../appsettings.json` as the filename. |
| Path traversal | **VULNERABLE** | `AdminController.Documents()` parses `fileName.Split('_')[0]` to extract userId — untrusted input drives a DB lookup and file deletion. |
| Model binding | **Safe** | `[Bind]` attributes restrict which properties can be set. `[Required]`, `[StringLength]`, `[Range]`, `[RegularExpression]` validations are present on models. |
| Over-posting | **Risky** | `OglasController.Edit` binds `Status` which allows clients to change the status field. `RecenzijaController.Edit` binds `RadnikId` and `KlijentId` which could let an admin change review ownership. |

## Secret Hygiene

| Check | Status |
|-------|--------|
| No hardcoded secrets | **FAIL** — `appsettings.json:3` contains `Password=postgres` in DefaultConnection. This is a committed connection string with credentials. |
| .env gitignored | **OK** — `.env` is listed in `.gitignore:15`. However, `.env` already exists in the repo root with real secrets (Stripe keys, Brevo API key) — it may have been committed before the gitignore rule was added. |
| appsettings.json safe | **PARTIAL** — Stripe/Email keys are empty (OK). But DefaultConnection contains real credentials (`Username=postgres;Password=postgres`). |
| appsettings.Development.json safe | **FAIL** — listed in `.gitignore:16` but contains real Stripe keys in committed file. Verify git history to confirm whether this was ever pushed. |
| Docker image clean | **OK** — Dockerfile uses multi-stage build. Runtime stage only contains published output. No secrets baked into image layers. |
| docker-compose.yml safe | **FAIL** — Contains hardcoded DB credentials (`POSTGRES_PASSWORD: postgres`) and admin password (`Admin__Password=${ADMIN_PASSWORD:-Admin123!}` with fallback default). The `Admin__Password` default `Admin123!` is a real password in production. |
| .env.example | **OK** — Has empty defaults, serves as safe template. |
| Stripe webhook secret | **OK** — Falls back to empty string in Program.cs, validated by Stripe signature check. |
| Password policy | **WEAK** — Identity configured with `RequireDigit=false, RequireLowercase=false, RequireNonAlphanumeric=false, RequireUppercase=false, RequiredLength=6`. Admin seed password is `Admin123!`. |

## Hardening Recommendations (Priority Order)

1. **CRITICAL: Add `[Authorize]` to ChatController** — All actions are currently accessible to anonymous users. Add `[Authorize]` at class level and verify user membership in chat for Poruke/PosaljiPoruku.

2. **CRITICAL: Add `[ValidateAntiForgeryToken]` to ChatController.PosaljiPoruku** — Missing CSRF protection on the only POST action that creates data in ChatController.

3. **CRITICAL: Fix path traversal in AdminController** — Sanitize `fileName` parameter: validate it contains only safe characters (alphanumeric, underscore, hyphen, dot), verify it exists under the documents directory, and use `Path.GetFileName()` to strip directory components.

4. **HIGH: Add `[Authorize]` to ObavijestController.Create** — Both GET and POST Create actions are unauthenticated. Any anonymous user can create notifications.

5. **HIGH: Remove credentials from appsettings.json** — Change DefaultConnection to use environment variable reference or remove the password entirely and rely on docker-compose environment injection.

6. **HIGH: Rotate all exposed secrets** — The `.env` file contains Stripe test keys and a Brevo API key. If this file was ever committed, rotate all keys immediately.

7. **HIGH: Remove default admin password** — The `Admin__Password` fallback in docker-compose.yml is `Admin123!`. Remove the default and require it to be set via environment variable.

8. **MEDIUM: Add `[Authorize]` to OglasController.UspjesnaPrijava/PrijavaGreska** — These should require the Radnik role since they're application outcome pages.

9. **MEDIUM: Add `[Authorize]` to RecenzijaController.Details** — Review details are publicly accessible.

10. **MEDIUM: Restrict ObavijestController.Create** — Add `[Authorize(Roles = "Admin")]` since creating notifications should be admin-only.

11. **MEDIUM: Remove debug TempData** — `RecenzijaController.Create` exposes debug session values and error messages via TempData which may leak sensitive info in production.

12. **LOW: Strengthen password policy** — Require at least one digit, one uppercase, and minimum length 8 for production deployments.
