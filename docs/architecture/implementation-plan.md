# Implementierungsplan

Reihenfolge ist bewusst so gewählt, dass jede Phase auf einem lauffähigen, testbaren Zwischenstand aufbaut ("vertical slice first, dann horizontal erweitern"). Wir beginnen mit **einem** Modul (`Customers`) End-to-End durch alle Schichten, bevor weitere Module folgen – das gibt früh ein funktionierendes Skelett und vermeidet, dass wir wochenlang nur Infrastruktur ohne sichtbares Ergebnis bauen.

| Phase | Inhalt | Ergebnis |
|---|---|---|
| **0** | Solution-/Projektskeleton anlegen: `src/`, `tests/`, Projektreferenzen gemäß Abhängigkeitsregel, `.editorconfig`, `.gitignore`, `Directory.Build.props`, NuGet-Paketverwaltung zentral | Leere, aber korrekt verdrahtete Solution, baut fehlerfrei |
| **1** | Domain-Schicht für **Customers**: Entities, Value Objects, Domain-Exceptions + `Banking.UnitTests`-Setup | Erste Unit-Tests auf reiner Geschäftslogik, ohne Framework-Abhängigkeit |
| **2** | Application-Schicht für **Customers**: Use Cases (z. B. `CreateCustomer`, `SearchCustomers`), DTOs, Repository-Interface, Validierung | Anwendungslogik testbar ohne Datenbank (Repository gemockt) |
| **3** | Infrastructure: EF Core `DbContext`, erste Migration, Repository-Implementierung, lokale Entwicklungsdatenbank (SQL Server LocalDB oder SQLite) | Persistenz funktioniert, Migration ausführbar |
| **4** | Api: Controller für Customers, DI-Verdrahtung (Composition Root), globale Exception-Middleware, Swagger/OpenAPI, API-Versionierung | Erste lauffähige REST-API, manuell testbar über Swagger |
| **5** | `Banking.IntegrationTests`: `WebApplicationFactory`, Tests gegen echte (Test-)Datenbank | Abgesicherter Regressionstest für den kompletten Customers-Slice |
| **6** | Web: Blazor Server Grundgerüst + QuickGrid (statt Telerik UI for Blazor, siehe ADR-Backlog), erste Seite "Kundenliste/-suche" über typisierten HttpClient gegen die Api | End-to-end sichtbares Feature: Kunde suchen im Browser |
| **7** | Security-Grundlage: Azure AD/Entra ID Authentifizierung, Autorisierungs-Policies, Secrets lokal über User Secrets (noch nicht Key Vault) | Login funktioniert, API abgesichert |
| **8** | Cross-Cutting fertigstellen: Serilog-Logging, strukturierte Fehlerantworten (ProblemDetails), Health-Check-Endpoint | Produktionsnahe Grundqualität etabliert, bevor weitere Module folgen |
| **9** | Weitere Module horizontal ergänzen: **Accounts**, dann **Transactions** (gleiches Muster wie Customers: Domain → Application → Infrastructure → Api → Web) | Kernbanking-Funktionalität wächst |
| **10** | **Payments**-Modul inkl. Statusverfolgung/Statushistorie (fachlich komplexer: Workflow/State Machine) | Zahlungsauftrag-Lifecycle abgebildet |
| **11** | **Integrations**-Modul: Interface-Abstraktion für SAP RFC (zunächst mit Fake/Stub-Implementierung), externer REST-Client mit Resilienz (Polly: Retry/Circuit Breaker) | Anti-Corruption-Layer nachweisbar, ohne echte SAP-Anbindung zu benötigen |
| **12** | **DataImports**-Modul + Anbindungskonzept an Azure Data Factory (Trigger/Status-Callback) | Import-Job-Überwachung im Portal sichtbar |
| **13** | **Auditing** & **Diagnostics**: Audit-Log-Interceptor (EF Core `SaveChanges`-Hook), Incident-Erfassung | Nachvollziehbarkeit und Fehleranalyse als Modul statt Nebensache |
| **14** | **Monitoring**-Dashboard: Health Checks aggregiert, Application-Insights-Kennzahlen im Portal darstellen | Systemstatus-Seite |
| **15** | Azure-Infrastruktur als Code (Bicep): App Service, Azure SQL (+ Elastic Pool), Key Vault, App Configuration, Application Insights, Managed Identity | Deploybare Umgebung reproduzierbar per Skript |
| **16** | CI/CD: Azure DevOps Pipelines (Build → Test → Security-Scan → Deploy), JFrog Artifactory für interne Pakete | Automatisierter Deployment-Workflow |
| **17** | Härtung: Performance (Paging, Indexing, Caching wo sinnvoll), Lasttest-Grundlagen, finaler Security-Review | Produktionsreife Qualität für ein Portfolio-Showcase |

**Wichtig:** Diese Tabelle ist der grobe Fahrplan – wir gehen sie Schritt für Schritt durch und passen sie an, wenn sich beim Bauen etwas anders sinnvoller zeigt. Nach jeder Phase kurze Retro: was war gut, was würden wir in echten Projekten anders machen (typische Enterprise-Fallstricke benennen).
