# Observability

## Warum OpenTelemetry statt des klassischen Application Insights SDK

Der ursprüngliche Plan ([Kapitel 10 im Anforderungsdokument](../prompt/ProjektErstellung.docx)) nennt Azure Application Insights. Wir erreichen dieses Ziel, aber über das **OpenTelemetry SDK** statt über das klassische `Microsoft.ApplicationInsights`-Paket:

- OpenTelemetry ist der herstellerneutrale Industriestandard (dieselbe Instrumentierung funktioniert unverändert mit Azure Monitor, AWS X-Ray, Grafana/Prometheus, Jaeger, ...). Microsoft selbst empfiehlt inzwischen diesen Weg für neue Projekte.
- Trace-Correlation (`ActivitySource`/`Activity`) und Custom Metrics (`Meter`/`Counter<T>`/`Histogram<T>`) sind reine **.NET-Bordmittel** aus `System.Diagnostics` - kein Zusatzpaket nötig, funktionieren auch ganz ohne registrierten Exporter (dann sind es günstige No-Ops).
- Dadurch lässt sich diese Kapitel - anders als Key Vault/Azure App Configuration in Kapitel 9 - **lokal vollständig verifizieren, ganz ohne Azure-Subscription** (siehe [decisions-backlog.md](../adr/decisions-backlog.md) #18).

## Bausteine

| Baustein | Technologie | Wo im Code |
|---|---|---|
| Request Telemetry | `OpenTelemetry.Instrumentation.AspNetCore` | automatisch, via `AddAspNetCoreInstrumentation()` |
| Dependency Telemetry (SQL) | `OpenTelemetry.Instrumentation.SqlClient` | automatisch, via `AddSqlClientInstrumentation()` |
| Dependency Telemetry (HTTP, z. B. Web → Api, später SAP) | `OpenTelemetry.Instrumentation.Http` | automatisch, via `AddHttpClientInstrumentation()` |
| Eigene Traces ("Payment Application Service"-Span) | `System.Diagnostics.ActivitySource` | [`Banking.Application/Common/Telemetry.cs`](../../src/Banking.Application/Common/Telemetry.cs), verwendet in [`CreatePaymentHandler`](../../src/Banking.Application/Payments/CreatePayment/CreatePaymentHandler.cs) |
| Exception Telemetry | `Activity.AddException()` + `SetStatus(Error)` | [`GlobalExceptionHandler`](../../src/Banking.Api/Middleware/GlobalExceptionHandler.cs) |
| Custom Metrics | `System.Diagnostics.Metrics.Meter` | `Telemetry.cs` (s. o.) |
| Strukturierte Logs mit Trace-Correlation | `ActivityTrackingOptions` (Bordmittel, kein Serilog) | `Banking.Api`/`Banking.Web` `Program.cs` |
| Export lokal | `OpenTelemetry.Exporter.Console` | immer aktiv |
| Export nach Azure | `Azure.Monitor.OpenTelemetry.Exporter` | nur aktiv, wenn `ApplicationInsights:ConnectionString` konfiguriert ist |

Das Gate-Muster ist identisch zu Kapitel 9: `Program.cs` liest `ApplicationInsights:ConnectionString`, und nur wenn dieser Wert gesetzt ist, wird der Azure-Monitor-Exporter zusätzlich zum immer aktiven Konsolen-Exporter registriert. Diese Verbindungszeichenfolge ist bewusst kein Secret im Key-Vault-Sinn (reiner Ingestion-Endpunkt, kein Lesezugriff auf Daten) und steht daher in `appsettings.json`.

## Custom Metrics im Detail

Definiert in `Telemetry.cs`, verwendet in den jeweiligen Application-Handlern:

- **`payment.success`** (Counter) - jede erfolgreich angelegte Zahlung
- **`payment.failed`** (Counter, Tag `reason`) - jede fehlgeschlagene Zahlungsauftrag-Anlage, mit Grund (`validation`, `account_not_found`, `account_not_active`, `invalid_currency`, `currency_mismatch`, `invalid_payment`) - so lässt sich in Application Insights sofort erkennen, *warum* Zahlungen scheitern, ohne Logs durchsuchen zu müssen
- **`payment.processing.duration`** (Histogram, ms) - von Validierungsbeginn bis Speichern, für Erfolgs- **und** Fehlerpfade
- **`transaction.search.duration`** (Histogram, ms) - Dauer der Transaktionssuche, in einem `finally`-Block erfasst, damit auch früh abbrechende Suchen (z. B. ungültige Parameter) in der Metrik landen

## Keine sensiblen Daten in Telemetrie

Bewusste Entscheidungen, um die Vorgabe "keine sensiblen Bankdaten und keine Secrets in Logs" einzuhalten:

- Activity-Tags enthalten nur **Ids** (`payment.source_account_id`, `payment.id`) - nie IBAN, Betrag oder Kundenname.
- `AddSqlClientInstrumentation()` erfasst in dieser Paketversion den rohen SQL-Text ohnehin nur nach explizitem Opt-in per Environment-Variable (nicht gesetzt) - Parameterwerte (die IBANs enthalten könnten) tauchen so nicht in Traces auf.
- Exception-Telemetrie (`Activity.AddException`) überträgt die .NET-Exception-Message/Stacktrace - bei Domain-Exceptions sind das kontrollierte, fachliche Meldungen ohne Kontodaten; bei technischen Exceptions (z. B. SQL-Verbindungsfehler) sind das Infrastruktur-Meldungen, ebenfalls ohne Kundendaten.

## Lokal verifiziert

Mit laufender Api (`ApplicationInsights:ConnectionString` leer → nur Konsolen-Exporter) wurde `POST /api/payments` aufgerufen. Der Konsolen-Exporter zeigte die vollständige, korrekt verschachtelte Span-Kette mit gemeinsamer `TraceId`:

```
Activity.DisplayName: POST api/payments        (Server-Span, ASP.NET-Core-Instrumentierung)
  └─ Activity.DisplayName: PaymentProcessing    (Internal-Span, eigene ActivitySource "Banking.Application")
       └─ Activity.DisplayName: INSERT          (Client-Span, SqlClient-Instrumentierung)
```

Alle drei Spans teilen sich dieselbe `Activity.TraceId` - genau die geforderte Nachvollziehbarkeit `POST /api/payments → Payment Application Service → SQL → (SAP, Kapitel 11) → Payment completed`. Zusätzlich bestätigt:

- Ein Zahlungsversuch auf ein nicht existierendes Konto erzeugte `payment.failed{reason=account_not_found}` mit `Value: 1`.
- Eine erfolgreiche Zahlung erzeugte `payment.success` sowie `payment.processing.duration` mit plausibler Dauer.
- `GET /api/transactions` erzeugte `transaction.search.duration`.
- Ein absichtlich provozierter SQL-Verbindungsfehler erschien sowohl als `aspnetcore.diagnostics.exceptions`-Metrik als auch als Span mit `StatusCode: Error` und `exception.type`/`exception.message`/`exception.stacktrace`-Tags auf dem Request-Span - die Exception-Telemetrie funktioniert also nachweislich auch für unerwartete, nicht selbst behandelte Fehler.

## Wie ich das im Bewerbungsgespräch erklären würde

> "Wir nutzen OpenTelemetry für die Observability - das ist der herstellerneutrale Standard, den Microsoft inzwischen selbst gegenüber dem klassischen Application-Insights-SDK empfiehlt. Für jeden eingehenden Request erzeugt die ASP.NET-Core-Instrumentierung automatisch einen Trace-Span; unsere Application-Schicht startet innerhalb dieses Requests einen eigenen Span für den fachlichen Schritt - zum Beispiel 'PaymentProcessing' - über eine reine .NET-Bordmittel-`ActivitySource`, ganz ohne Kopplung an ASP.NET Core. Jeder nachgelagerte Aufruf, egal ob SQL oder später ein SAP-Call über HTTP, wird automatisch als Kind-Span in denselben Trace eingehängt, weil .NET die Trace-Correlation (`Activity.Current`) über Async-Kontexte hinweg selbst durchreicht. Ergebnis: in Application Insights sehe ich für einen einzelnen `POST /api/payments`-Aufruf die komplette Kette bis zur einzelnen SQL-Query, mit derselben Trace-Id, ohne dass ich manuell irgendeine Correlation-Id durchreichen musste.
>
> Für Metriken haben wir vier fachliche Kennzahlen definiert - `payment.success`, `payment.failed` mit einem Tag für den Fehlergrund, sowie Verarbeitungsdauer für Zahlungen und Transaktionssuche. Das sind ganz normale .NET-`Meter`/`Counter`/`Histogram`-Instrumente, die OpenTelemetry einsammelt und weiterleitet - so kann ich in Application Insights ein Dashboard bauen, das mir zeigt, *warum* Zahlungen scheitern, nicht nur *dass* etwas schiefläuft.
>
> Beim Thema Datenschutz war uns wichtig, dass keine sensiblen Bankdaten in die Telemetrie gelangen - Tags enthalten nur IDs, nie IBAN oder Beträge, und die SQL-Instrumentierung erfasst standardmäßig keinen Query-Text mit Parameterwerten.
>
> Lokal exportieren wir alles einfach auf die Konsole, ganz ohne Azure - erst wenn eine echte Application-Insights-Connection-String konfiguriert ist, aktiviert sich zusätzlich der Azure-Monitor-Exporter. Das ist dasselbe Muster, das wir schon bei Key Vault und App Configuration verwendet haben: Code ist produktionsreif vorbereitet, aber lokal ohne Azure-Abhängigkeit vollständig testbar."

Mögliche Interviewer-Rückfragen und Antworten:

- **"Warum nicht Application Insights SDK direkt?"** → Legacy-Pfad, herstellergebunden; OpenTelemetry ist der Industriestandard und Microsofts eigene aktuelle Empfehlung; gleicher Code würde auch mit einem anderen Backend funktionieren.
- **"Was, wenn niemand die Traces anschaut?"** → Ohne registrierten Listener/Exporter sind `ActivitySource.StartActivity()` und `Meter`-Instrumente günstige No-Ops - kein spürbarer Overhead im Normalbetrieb.
- **"Wie verhindert ihr, dass sensible Daten in Application Insights landen?"** → Bewusste Tag-Auswahl (nur IDs), keine SQL-Parameterwerte in Traces, siehe Abschnitt oben.
- **"Was passiert bei einem verteilten Aufruf über mehrere Services (Web → Api)?"** → W3C-`traceparent`-Header werden automatisch von der HttpClient-Instrumentierung mitgeschickt/gelesen, solange auf beiden Seiten mindestens die ASP.NET-Core-/HttpClient-Instrumentierung aktiv ist (bei uns auch in `Banking.Web` registriert) - der Trace bleibt dienstübergreifend zusammenhängend.
