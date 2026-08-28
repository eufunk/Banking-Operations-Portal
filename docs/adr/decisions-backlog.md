# Architekturentscheidungen (Backlog)

Diese Liste sammelt die wichtigsten Entscheidungen, die wir im Projektverlauf treffen. Sobald wir eine Entscheidung tatsächlich umsetzen, wird sie als eigenes ADR-Dokument (`docs/adr/0001-....md` usw., nach dem gängigen ADR-Format: Kontext, Entscheidung, Konsequenzen) ausformuliert. Hier zunächst nur die Kurzfassung zur Orientierung.

| # | Entscheidung | Kurzbegründung |
|---|---|---|
| 1 | **Modular Monolith statt Microservices** | Passt zum Projektumfang, vermeidet Infrastruktur-Overhead, entspricht typischen Einstiegs-/Mittelstands-Enterprise-Setups im Bankenumfeld |
| 2 | **Blazor Web spricht Api ausschließlich über REST/HTTP an**, kein In-Process-Zugriff auf Application | Zeigt echtes API-Design, hält UI/Backend sauber getrennt, API bleibt für weitere Clients wiederverwendbar |
| 3 | **EF Core, Code-First mit Migrations** | Standard in .NET-Enterprise-Umgebungen, gute Interviewrelevanz, Migrations dokumentieren Schema-Historie |
| 4 | **Kein generisches Repository<T> über EF Core** | `DbContext` ist bereits Unit of Work + Repository; ein zusätzliches generisches Repository verdeckt EF-Core-Fähigkeiten (Includes, Projektionen) ohne echten Mehrwert – stattdessen fokussierte, use-case-orientierte Repository-Interfaces pro Aggregat |
| 5 | **DTOs statt Domain-Objekte über die API**, Mapping explizit (Mapster oder manuelles Mapping) | Verhindert Over-Posting/Under-Posting, entkoppelt API-Vertrag von internem Domain-Modell |
| 6 | **Validierung mit FluentValidation in der Application-Schicht** | Zentral, testbar, unabhängig von Controller/UI |
| 7 | **Strukturierte Fehlerbehandlung via ProblemDetails (RFC 7807)** + globale Exception-Middleware | Konsistente, maschinenlesbare Fehlerantworten, kein Stacktrace-Leak |
| 8 | **Authentifizierung über Azure AD/Entra ID (OpenID Connect)** statt eigenem Identitätssystem | Realistisch für Enterprise/Bankenumfeld, kein Passwort-Handling in eigener Verantwortung |
| 9 | **SAP-Anbindung über Interface-Abstraktion (Anti-Corruption Layer)** | Fachliche Logik bleibt unabhängig von SAP-RFC-Details, austauschbar/mockbar für Tests |
| 10 | **Secrets ausschließlich über Key Vault + Managed Identity** | Keine Secrets im Repo, keine manuelle Credential-Verteilung |
| 11 | **API-Versionierung über URL-Segment (`/api/v1/...`)** | Explizit, leicht verständlich, gut sichtbar in Doku/Tests |
| 12 | **Logging mit Serilog, strukturiert, Sink nach Application Insights** | Korrelation über mehrere Schichten/Requests hinweg möglich |
| 13 | **Payment-Status als explizite State Machine**, nicht als freies Enum-Feld | Verhindert ungültige Statusübergänge (z. B. `Executed` → `Draft`), zentrale Stelle für Transitions-Regeln |
| 14 | **Audit-Logging via EF Core `SaveChanges`-Interceptor**, nicht manuell pro Use Case | Konsistent, kann nicht vergessen werden, ein zentraler Ort für Nachvollziehbarkeit |

Diese Reihenfolge ist keine Priorisierung, sondern grob an der Reihenfolge im [Implementierungsplan](../architecture/implementation-plan.md) orientiert. Wir formalisieren jede Entscheidung als eigenes ADR, sobald die zugehörige Phase ansteht – inklusive Alternativen, die wir verworfen haben, und den jeweiligen Trade-offs.
