# Secrets Rotation Guide

## Overview
This document describes how to rotate secrets for the NaPoso application.

## Secrets Inventory

| Secret | Location | Purpose |
|--------|----------|---------|
| `STRIPE_SECRET_KEY` | `.env` | Stripe payment processing |
| `STRIPE_PUBLISHABLE_KEY` | `.env` | Stripe client-side integration |
| `STRIPE_WEBHOOK_SECRET` | `appsettings.json` | Stripe webhook verification |
| `EMAIL_BREVO_API_KEY` | `.env` | Brevo email service |
| `POSTGRES_PASSWORD` | `.env` | Database password |

## Rotation Procedures

### 1. Stripe Keys
1. Log in to [Stripe Dashboard](https://dashboard.stripe.com)
2. Navigate to **Developers > API Keys**
3. Click **Roll key** next to the secret key
4. Update `.env` with the new `STRIPE_SECRET_KEY`
5. Update webhook endpoint secret in Stripe Dashboard
6. Update `appsettings.json` or environment variable for `Stripe:WebhookSecret`
7. Test payment flow in staging before deploying to production

### 2. Brevo API Key
1. Log in to [Brevo Dashboard](https://app.brevo.com)
2. Navigate to **SMTP & API > API Keys**
3. Generate a new API key
4. Update `.env` with the new `EMAIL_BREVO_API_KEY`
5. Revoke the old key after verification

### 3. Database Password
1. Connect to PostgreSQL as admin
2. Run: `ALTER USER postgres WITH PASSWORD 'new_password';`
3. Update `POSTGRES_PASSWORD` in `.env`
4. Update `ConnectionStrings:DefaultConnection` if not using env vars
5. Restart the application

## Environment Setup

### Local Development
```bash
# Copy the template and fill in values
cp .env.example .env
```

### Production (Docker)
```bash
# Pass secrets via environment variables or Docker secrets
docker run -e STRIPE_SECRET_KEY=sk_live_... naposo
```

### CI/CD
Store secrets in your CI provider's secret manager (GitHub Actions Secrets, etc.).
Never commit `.env` or `appsettings.Development.json` to version control.

## Verification Checklist
- [ ] Application starts without errors
- [ ] Payment processing works end-to-end
- [ ] Emails are sent successfully
- [ ] Database connections are stable
- [ ] Old keys are revoked/rotated
