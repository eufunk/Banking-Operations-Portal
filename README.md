# Banking Operations Portal

Ein internes Portal, über das Bankmitarbeiter Kunden, Konten und Transaktionen überwachen, Zahlungsaufträge bearbeiten, Fehlerfälle untersuchen und Import-Jobs/Audit-Logs verwalten können.

Portfolio-Projekt zur Vorbereitung auf .NET/C#/Azure-Enterprise-Entwicklung im Bankenumfeld. Architektur, Modulzuschnitt und Implementierungsplan stehen in [`docs/architecture/`](docs/architecture/), Architekturentscheidungen in [`docs/adr/`](docs/adr/).

## Tech-Stack

- **Backend:** .NET 10 (LTS), ASP.NET Core Web API, Entity Framework Core
- **Frontend:** Blazor Server, Microsoft.AspNetCore.Components.QuickGrid (statt Telerik UI for Blazor – kommerzielle Lizenz war nicht zugänglich, siehe [ADR-Backlog](docs/adr/decisions-backlog.md))
- **Architektur:** Modular Monolith nach Clean-Architecture-Prinzipien
- **Cloud:** Azure App Service, Azure SQL, Key Vault, App Configuration, Application Insights, Data Factory
- **Tests:** xUnit (Unit- und Integrationstests)

## Projektstruktur

```
src/
  Banking.Domain/          Entities, Value Objects, Geschäftsregeln – keine Framework-Abhängigkeiten
  Banking.Application/     Use Cases, DTOs, Interfaces für Repositories/externe Systeme
  Banking.Infrastructure/  EF Core, Repository-Implementierungen, externe Integrationen
  Banking.Api/             ASP.NET Core Web API (Controller, DI-Verdrahtung)
  Banking.Web/             Blazor Server UI, spricht die Api ausschließlich über HTTP an
tests/
  Banking.UnitTests/
  Banking.IntegrationTests/
docs/
  architecture/            Gesamtarchitektur, Module, Domain-Entities, Implementierungsplan, Azure-Services
  adr/                     Architecture Decision Records
```

Abhängigkeitsrichtung und Begründung: siehe [`docs/architecture/overview.md`](docs/architecture/overview.md). Configuration-/Secrets-Strategie (lokal und für Azure vorbereitet): siehe [`docs/architecture/configuration.md`](docs/architecture/configuration.md). Observability (Tracing/Metriken, OpenTelemetry): siehe [`docs/architecture/observability.md`](docs/architecture/observability.md). SAP-Integration (Anti-Corruption Layer, Mock/HttpClient-Adapter): siehe [`docs/architecture/sap-integration.md`](docs/architecture/sap-integration.md). Transaktions-Import (simulierte Azure-Data-Factory-Pipeline): siehe [`docs/architecture/data-import.md`](docs/architecture/data-import.md). Unit-Test-Strategie (was getestet wird, was nicht, und warum): siehe [`docs/architecture/testing.md`](docs/architecture/testing.md).

## Lokales Setup (einmalig)

Voraussetzung: [.NET 10 SDK](https://dotnet.microsoft.com/download) + SQL Server LocalDB (Teil der VS-Installation) oder SQL Server in Docker.

**1. Datenbank-Connection-String** ist für LocalDB bereits in `appsettings.Development.json` hinterlegt (Windows Integrated Security, kein Secret nötig). Bei Docker-SQL-Server stattdessen selbst setzen:
```bash
dotnet user-secrets set "ConnectionStrings:BankingDatabase" "...;User Id=sa;Password=..." --project src/Banking.Api
```

**2. JWT-Signing-Key** (Secret, nicht in appsettings/Git) für die Web↔Api-Authentifizierung erzeugen und in **beiden** Projekten identisch hinterlegen:
```bash
# Beliebiger zufälliger String, z. B. per PowerShell:
# [Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Max 256 }))
dotnet user-secrets set "Jwt:SigningKey" "<dein-zufaelliger-key>" --project src/Banking.Api
dotnet user-secrets set "Jwt:Issuer" "BankingOperationsPortal" --project src/Banking.Api
dotnet user-secrets set "Jwt:Audience" "BankingOperationsPortal.Api" --project src/Banking.Api

dotnet user-secrets set "Jwt:SigningKey" "<derselbe-key>" --project src/Banking.Web
dotnet user-secrets set "Jwt:Issuer" "BankingOperationsPortal" --project src/Banking.Web
dotnet user-secrets set "Jwt:Audience" "BankingOperationsPortal.Api" --project src/Banking.Web
```

Ohne Schritt 2 startet die Api mit einer `InvalidOperationException` beim Hochfahren (bewusst - lieber beim Start scheitern als mit einem hartkodierten Fallback-Schlüssel laufen).

## Solution bauen und starten

```bash
# Solution wiederherstellen und bauen
dotnet build

# Tests ausführen
dotnet test

# Api starten (Standard-Port siehe Properties/launchSettings.json)
dotnet run --project src/Banking.Api

# Web-Frontend starten
dotnet run --project src/Banking.Web
```

Demo-Login im Web-Frontend: `employee` / `manager` / `admin`, Passwort jeweils `Demo123!` (nur lokale Testkonten, siehe [`docs/architecture/configuration.md`](docs/architecture/configuration.md) und den Sicherheitshinweis dort).
