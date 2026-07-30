# NaPoso — potpuni reset PostgreSQL baze
# Brise sve podatke i pri sljedecem pokretanju aplikacije kreira shemu + seed podatke.

param(
    [string]$DbHost = "localhost",
    [int]$Port = 5432,
    [string]$Database = "naposo",
    [string]$Username = "postgres",
    [string]$Password = "postgres",
    [switch]$UseDocker
)

$ErrorActionPreference = "Stop"

Write-Host "=== NaPoso: reset baze '$Database' ===" -ForegroundColor Cyan

if ($UseDocker) {
    Write-Host "Zaustavljam Docker Compose i brisem volume (pgdata)..." -ForegroundColor Yellow
    Push-Location (Split-Path $PSScriptRoot -Parent)
    docker compose down -v
    Write-Host "Pokrecem Docker Compose..." -ForegroundColor Yellow
    docker compose up --build -d
    Pop-Location
    Write-Host "Gotovo. Aplikacija ce pri startu kreirati bazu i seed podatke." -ForegroundColor Green
    Write-Host "URL: http://localhost:5000" -ForegroundColor Green
    exit 0
}

$env:PGPASSWORD = $Password

Write-Host "Brisem bazu '$Database'..." -ForegroundColor Yellow
& psql -h $DbHost -p $Port -U $Username -d postgres -c "DROP DATABASE IF EXISTS `"$Database`" WITH (FORCE);"
& psql -h $DbHost -p $Port -U $Username -d postgres -c "CREATE DATABASE `"$Database`";"

Write-Host "Baza je prazna. Pokreni aplikaciju da se kreira shema i seed:" -ForegroundColor Green
Write-Host "  dotnet run --project NaPoso/NaPoso" -ForegroundColor White
