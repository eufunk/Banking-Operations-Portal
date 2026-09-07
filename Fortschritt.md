# Fortschritt

Stand: 2026-09-07. Diese Datei fasst zusammen, was im Banking Operations Portal bereits umgesetzt und getestet ist, was nur teilweise vorbereitet ist, und was laut dem ursprünglichen Anforderungsdokument ([docs/prompt/ProjektErstellung.docx](docs/prompt/ProjektErstellung.docx), 24 Kapitel + Master-Prompt) noch fehlt. Sie wird bei Bedarf manuell aktualisiert, ist also eine Momentaufnahme, kein Live-Dashboard.

Alle Abweichungen vom ursprünglichen Plan sind zusätzlich an zwei Stellen dokumentiert: direkt im betroffenen Kapitel in `ProjektErstellung.docx` ("Update / Umsetzung") und als nummerierter Eintrag in [docs/adr/decisions-backlog.md](docs/adr/decisions-backlog.md) (aktuell 20 Einträge).

## Umgesetzt (Kapitel 1–12 von 24)

| # | Kapitel | Umgesetzt als | Wie verifiziert |
|---|---|---|---|
| 1 | Projekt initialisieren | Solution mit 5 Projekten (Domain/Application/Infrastructure/Api/Web) + 2 Testprojekten, Central Package Management (`Directory.Packages.props`) | `dotnet build` |
| 2 | Domain Model | Entities `Customer`, `Account`, `Transaction`, `Payment`; Value Objects (`Money`, `CurrencyCode`, `AccountNumber`, `CustomerNumber`, `Email`); strongly-typed IDs | `dotnet build`, manuell über Api geprüft |
| 3 | Entity Framework + SQL Server | `BankingDbContext`, Fluent-API-Configurations, Migration `InitialCreate`, LocalDB lokal | Migration erfolgreich gegen LocalDB angewendet, Api liest/schreibt live |
| 4 | Application Layer | Command/Query-Pattern (eigen, kein MediatR), `Result<T>`/`Error`, Repository-Interfaces, FluentValidation | Über REST-Endpunkte live getestet |
| 5 | REST API | `Banking.Api` mit Controllers für Customers/Accounts/Transactions/Payments, ProblemDetails (RFC 7807), globaler Exception-Handler | Live per curl gegen laufende Api getestet |
| 6 | Blazor Server | `Banking.Web`: Dashboard, Kunden-/Konten-/Transaktions-/Zahlungs-Seiten, BFF-Pattern (Web spricht nur REST mit Api) | Im Browser manuell durchgeklickt |
| 7 | Telerik UI | **Abweichung**: `Microsoft.AspNetCore.Components.QuickGrid` statt Telerik (kommerzielle Lizenz nicht verfügbar) – ADR #15 | Im Browser geprüft (Sortierung/Paging/Filter) |
| 8 | Authentication & Authorization | Cookie-Auth (Web) + JWT (Api), Rollenmodell BankEmployee/OperationsManager/Administrator mit kumulativen Claims. **Abweichung**: lokale Demo-Nutzer statt Azure AD/Entra ID (keine Azure-Subscription) – ADR #16 | Live mit selbst erzeugten JWTs pro Rolle gegen alle Endpunkte getestet |
| 9 | Azure Key Vault + Configuration | Key Vault/App Configuration im Code vorbereitet (`DefaultAzureCredential`), aber inaktiv ohne Subscription; lokal: appsettings + User Secrets + Env Vars – ADR #17 | Lokale Konfigurationskette live geprüft; Azure-Pfad nur code-review-geprüft (kein Azure-Zugang) |
| 10 | Application Insights | **Abweichung**: OpenTelemetry SDK statt klassischem Application-Insights-SDK, Konsolen-Exporter immer aktiv, Azure-Monitor-Exporter nur bei konfigurierter Connection String – ADR #18 | Live verifiziert: vollständige Trace-Kette `POST /api/payments → PaymentProcessing → SQL` mit gemeinsamer Trace-Id, alle 4 Custom Metrics, Exception Telemetry bei erzwungenem SQL-Fehler |
| 11 | SAP RFC Integration | Anti-Corruption Layer (`ISapCustomerService`), `MockSapCustomerService` (lokaler Standard) + `SapRfcCustomerService` (echter HttpClient-Adapter mit Resilience-Handler statt SAP .NET Connector, der nicht installierbar ist) – ADR #19 | Live verifiziert: Mock-Pfad UND Graceful-Degradation-Pfad (SAP absichtlich nicht erreichbar → `sapAvailable: false` statt Fehler) |
| 12 | Azure Data Factory | Simulierte CSV-Import-Pipeline (`ImportJob`/`ImportRecord`, erstes Aggregat mit Kind-Collection), Validation-/Transformation-Schritte als echter Code, Idempotenz über externe TransactionId, Blazor-Seite `/imports` – ADR #20 | Live verifiziert: Migration erfolgreich angewendet, Testdatei mit gültigen/ungültigen Zeilen korrekt verarbeitet (2 erfolgreich, 3 fehlerhaft mit nachvollziehbaren Fehlermeldungen), erzeugte Transaktionen im Konto bestätigt, Idempotenz bei erneutem Upload bestätigt (Skipped statt Doppelbuchung), Rollenmodell bestätigt (BankEmployee: 403 bei Upload, 200 bei Ansicht) |

Zusätzlich erstellt, aber außerhalb der 24 Kapitel: eine separat gepflegte [Einsteiger-Dokumentation](docs/Einsteiger-Dokumentation.docx) für technisch weniger erfahrene Leser, inzwischen mit drei Architektur-Diagrammen (Schichtenmodell, Request-/Auth-Fluss, System-Kontext) illustriert.

## Bekannte offene Punkte aus bereits umgesetzten Kapiteln

- **Kapitel 12 – bewusste Vereinfachung**: Import läuft synchron im Request statt asynchron über eine Queue (kein Hangfire/Quartz eingeführt) – dokumentiert, nicht als Fehler zu werten.
- **Kein automatisierter Test deckt bisher irgendeine der Kapitel 1–12 ab** (siehe Kapitel 16/17 unten) – die gesamte bisherige Verifikation ist manuelles/Live-Testen während der jeweiligen Session gewesen, keine wiederholbare Testsuite.

## Noch zu tun (Kapitel 13–24)

| # | Kapitel | Status |
|---|---|---|
| 13 | Azure SQL Elastic Pool | Nicht begonnen. Nur als Erwähnung in `docs/architecture/implementation-plan.md` vorhanden, kein Code, keine IaC-Datei (Bicep/ARM/Terraform existiert im Repo überhaupt noch nicht). |
| 14 | JFrog | Nicht begonnen. Keine Konfiguration, kein Hinweis im Repo. |
| 15 | Azure DevOps CI/CD | Nicht begonnen. Kein `.github/workflows/`, kein `azure-pipelines.yml`, keine `.azuredevops/`-Struktur vorhanden – es existiert noch keine einzige CI/CD-Pipeline-Datei im Repo. |
| 16 | Unit Tests | **Nur Scaffold.** `tests/Banking.UnitTests` enthält ausschließlich die generierte Vorlage (`UnitTest1.cs`, ein leerer `[Fact]`). Keine Mocking-Bibliothek referenziert (kein Moq/NSubstitute). Kein einziger Handler/Domain-Test existiert bisher. |
| 17 | Integration Tests | **Nur Scaffold.** `tests/Banking.IntegrationTests` ebenso nur die leere Vorlage. Kein `WebApplicationFactory`, kein Testcontainers/InMemory-Provider referenziert. |
| 18 | Audit Logging | **Teilweise vorbereitet, nicht funktional.** `AuditLogEntry`-Entity und EF-Core-Konfiguration existieren bereits (auch in der Datenbank-Migration), aber es gibt keinen `SaveChanges`-Interceptor und keine Stelle im Code, die `AuditLogEntry.Create(...)` tatsächlich aufruft. Die Tabelle bliebe also leer. |
| 19 | Legacy-Code + Refactoring | Nicht begonnen. |
| 20 | Security Review | Nicht begonnen. Kein `docs/architecture/security*.md`. |
| 21 | Performance Review | Nicht begonnen. Kein `docs/architecture/performance*.md`. |
| 22 | Architecture Decision Records | **Teilweise vorhanden.** `docs/adr/decisions-backlog.md` mit 20 nummerierten Einträgen dient bisher als informelles ADR-Log. Individuelle, formale ADR-Dokumente (z. B. `docs/adr/0001-....md` im Standardformat) wurden noch nicht angelegt. |
| 23 | Finaler Code Review | Nicht begonnen. |
| 24 | Interview-Simulation | Nicht begonnen. |

## Empfohlene nächste Schritte

1. **Kapitel 16/17 (Tests) vorziehen**, bevor weitere Kapitel den Umfang noch weiter vergrößern – aktuell gibt es keinerlei Regressionsschutz für die bereits gebauten 12 Kapitel.
2. Danach chronologisch mit Kapitel 13 (Azure SQL Elastic Pool) fortfahren, mit derselben Transparenz wie bisher: reale, lokal lauffähige Bausteine bauen, wo eine echte Azure-Ressource fehlt, und jede Abweichung in ADR + Kapitel-Notiz dokumentieren.
