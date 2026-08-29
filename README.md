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

Abhängigkeitsrichtung und Begründung: siehe [`docs/architecture/overview.md`](docs/architecture/overview.md).

## Solution bauen und starten

Voraussetzung: [.NET 10 SDK](https://dotnet.microsoft.com/download).

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
