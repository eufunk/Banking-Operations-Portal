# Tests (Kapitel 16 & 17)

## Unit Tests (Kapitel 16)

### Werkzeuge

- **xUnit** - war bereits als Testframework eingerichtet (Central Package Management), nur mit der leeren Standard-Vorlage.
- **NSubstitute** (neu) - für Mocks der Repository-Interfaces. Bewusst **nur externe Abhängigkeiten** gemockt (Datenbank-Zugriffe über die Repository-Interfaces) - FluentValidation-Validatoren sind reine, schnelle In-Process-Logik und werden **echt** ausgeführt, nicht gemockt. Das ist genauer als ein reiner Handler-Test mit immer-gültigem Validator-Mock, weil es echte FluentValidation-Regeln (z. B. das Zusammenspiel mehrerer Regeln) mitprüft.

Alle Tests laufen ohne Datenbank, ohne Netzwerk, ohne laufende Anwendung - **28 Tests in unter einer Sekunde** (`dotnet test`), wie von Kapitel 16 gefordert ("schnell und deterministisch"). Dieser Lauf wurde direkt nach dem Schreiben der Tests tatsächlich ausgeführt und war grün; nach dem späteren Gesamt-Build für Kapitel 17 blockierte McAfees Anwendungssteuerungsrichtlinie die neu kompilierte `Banking.UnitTests.dll` erneut (dasselbe Umgebungsproblem wie bei `Banking.IntegrationTests.dll`, siehe unten) - der Code hat sich seitdem nicht geändert, ein erneuter Lauf steht nur wegen dieser Blockade noch aus.

### Aufbau: drei Test-Ebenen für "Payment Processing"

| Ebene | Datei | Was getestet wird | Mocking |
|---|---|---|---|
| Domain | `tests/Banking.UnitTests/Domain/Payments/PaymentTests.cs` | Statusübergänge des `Payment`-Aggregats (State Machine), `Payment.Create`-Guards | keins |
| Application (Validator) | `tests/Banking.UnitTests/Payments/CreatePaymentValidatorTests.cs` | FluentValidation-Regeln (Betrag ≤ 0, Betrag über Limit, Pflichtfelder) | keins |
| Application (Handler) | `tests/Banking.UnitTests/Payments/CreatePaymentHandlerTests.cs` | Geschäftsregeln, die einen Datenbankzugriff brauchen (Konto/Kunde laden, Kontostand, Duplikat-Prüfung) | Repositories + Validator gemockt |

### Vorher fehlende Business Rules: jetzt implementiert

Beim Abgleich der von Kapitel 16 geforderten Testfälle mit dem bestehenden Code fiel auf, dass drei Regeln schlicht noch nicht existierten - sie wurden ergänzt, bevor sie getestet werden konnten (siehe [decisions-backlog.md](../adr/decisions-backlog.md) #21):

- **Inaktiver Kunde**: `CreatePaymentHandler` prüfte bisher nur den Kontostatus, nie den Status des zugehörigen Kunden. Ein Konto kann aktiv sein, während der Kunde nachträglich gesperrt wurde.
- **Unzureichender Kontostand**: Der eigentliche Kontostand-Abzug passiert erst bei der Ausführung einer Zahlung (`Account.Debit`, noch nicht implementiert) - trotzdem sollte ein offensichtlich ungedeckter Auftrag nicht einmal angelegt werden können. Kreditkonten sind bewusst ausgenommen (dürfen laut Domain-Modell ins Minus laufen).
- **Doppelte Zahlung**: Schutz vor versehentlicher Doppel-Einreichung (z. B. Doppelklick) über ein kurzes Zeitfenster (1 Minute) auf exakt gleiche Eckdaten (Quellkonto, Zielkonto, Betrag, Währung).

Alle drei live gegen die laufende Anwendung verifiziert (nicht nur über Unit Tests): `Payment.InsufficientFunds`, `Payment.Duplicate` und `Customer.NotActive` liefern jeweils den erwarteten HTTP 409 mit nachvollziehbarer Fehlermeldung.

### Welche Business Rules getestet werden

| Kapitel-16-Testfall | Wo getestet | Testname |
|---|---|---|
| Payment amount ≤ 0 | Validator | `Validate_WithAmountLessOrEqualZero_IsInvalid` (+ Domain: `Create_WithZeroAmount_Throws`, `Create_WithNegativeAmount_Throws`) |
| Payment amount > allowed limit | Validator | `Validate_WithAmountAboveConfiguredLimit_IsInvalid` |
| inactive customer | Handler | `Handle_WhenCustomerInactive_ReturnsConflict` |
| inactive source account | Handler | `Handle_WhenSourceAccountBlocked_ReturnsConflict` |
| insufficient balance | Handler | `Handle_WhenBalanceInsufficient_ReturnsConflict` (+ Gegenprobe: `Handle_ForCreditAccount_AllowsPaymentBeyondBalance`) |
| successful payment | Handler | `Handle_WhenAllChecksPass_ReturnsSuccessAndPersistsPayment` |
| duplicate payment | Handler | `Handle_WhenSimilarPaymentWasJustSubmitted_ReturnsConflict` |
| invalid currency | Handler | `Handle_WhenCurrencyNotSupported_ReturnsValidationError` |
| invalid target account | Handler | `Handle_WhenTargetAccountNumberInvalid_ReturnsValidationError` |
| invalid payment status transition | Domain | `Approve_FromDraft_ThrowsInvalidStatusTransition`, `Reject_FromApproved_ThrowsInvalidStatusTransition`, `MarkExecuted_FromApproved_WithoutSubmittedToNetwork_ThrowsInvalidStatusTransition` |

Zusätzlich, über die geforderte Liste hinaus: `Handle_WhenSourceAccountNotFound_ReturnsNotFound`, `Handle_WhenCurrencyDoesNotMatchAccountCurrency_ReturnsConflict`, `Handle_WhenApprovalWorkflowEnabled_SubmitsPaymentForApproval`, `Handle_WhenCommandInvalid_ReturnsValidationFailure`, vollständiger Payment-Lifecycle (Draft → Executed).

### Welche Business Rules NICHT getestet werden

- **Alles, was eine echte Datenbank oder einen echten HTTP-Request braucht**: dass die EF-Core-Konfiguration korrekt ist (Spalten, Indizes, Fremdschlüssel), dass eine Migration tatsächlich anwendbar ist, dass das Optimistic-Concurrency-Verhalten (`RowVersion`) bei echten parallelen Schreibzugriffen greift. Das ist explizit Aufgabe der **Integration Tests** (Kapitel 17), nicht der Unit Tests.
- **Die Freigabe-Workflow-Endpunkte selbst** (`ApprovePaymentHandler`, `RejectPaymentHandler`) - noch ohne eigene Unit Tests, da sie strukturell sehr ähnlich zu `CreatePaymentHandler` sind (kein neues Testmuster nötig) und aus Zeitgründen für dieses Kapitel zurückgestellt wurden.
- **Andere Aggregate** (`Account`, `Customer`, `Transaction`, `ImportJob`) - Kapitel 16 verlangt explizit "die wichtigsten Business Rules, insbesondere Payment Processing"; die anderen Aggregate haben strukturell ähnliche State-Machine-Guards, die nach demselben Muster ergänzt werden können, sobald sie an der Reihe sind.
- **Nebenläufigkeit** (zwei gleichzeitige Freigaben desselben Zahlungsauftrags) - dafür bräuchte es entweder Integration Tests gegen eine echte Datenbank oder gezielte Concurrency-Tests, die über den Rahmen eines einfachen Unit Tests hinausgehen.
- **Die Duplikat-Erkennung als tatsächliche SQL-Abfrage** - der Unit Test prüft nur, dass der Handler bei `ExistsSimilarRecentAsync() == true` korrekt ablehnt; dass die SQL-Abfrage selbst (Übersetzung des LINQ-Ausdrucks durch EF Core, insbesondere der Vergleich der `ComplexProperty` `Money`) tatsächlich das Richtige tut, wurde nur live gegen LocalDB geprüft (siehe oben), nicht durch einen automatisierten Test.

### Warum Unit Tests die Integration Tests nicht ersetzen

Ein Unit Test wie in diesem Kapitel beweist: *"Wenn das Konto inaktiv ist, gibt der Handler `Account.NotActive` zurück"* - unter der Annahme, dass `IAccountRepository.GetByIdAsync` sich so verhält, wie es das Mock vorgibt. Er beweist **nicht**:

- dass `AccountRepository` (die echte EF-Core-Implementierung) tatsächlich das richtige Konto aus der echten Datenbank lädt,
- dass die HTTP-Pipeline (Routing, `[Authorize(Roles=...)]`, Model Binding, JSON-Serialisierung) den Request überhaupt bis zum Handler durchlässt,
- dass mehrere Schichten zusammenpassen (z. B. dass eine Änderung an der EF-Core-Konfiguration nicht versehentlich ein Feld falsch mappt, das der Unit Test gar nicht anfasst, weil er die Datenbank komplett durch ein Mock ersetzt),
- dass die Migration, die eine neue Spalte/Tabelle anlegt, überhaupt fehlerfrei gegen eine echte SQL-Server-Instanz läuft.

Unit Tests sind **schnell und isolieren die Fehlerursache** (schlägt ein Test fehl, weiß man sofort: die Logik in genau dieser einen Klasse ist falsch). Integration Tests sind **langsamer, aber realistischer** (echte Datenbank oder zumindest `WebApplicationFactory`) und decken genau die Lücken ab, die beim Mocken der Repositories entstehen. Beide zusammen ergeben erst ein vollständiges Sicherheitsnetz - das ist auch der Grund, warum Kapitel 17 (Integration Tests) im Anforderungsplan direkt auf dieses Kapitel folgt.

## Integration Tests (Kapitel 17)

### Werkzeuge

- **Microsoft.AspNetCore.Mvc.Testing** (`WebApplicationFactory<Program>`) - startet die echte Api In-Process (ASP.NET Core `TestServer`): echte Dependency Injection, echte Middleware-Pipeline (Routing, Authentication/Authorization, ProblemDetails-Fehlerbehandlung), echte Application-Handler. Genau das verlangt die Vorgabe "Vermeide Mocking der kompletten Application Layer" - hier wird **nichts** von alldem gemockt, nur der Prozess/das Netzwerk wird übersprungen (In-Process statt echtem Socket).
- **System.IdentityModel.Tokens.Jwt** - baut echte, gültig signierte Test-JWTs mit derselben Bibliothek, die auch `Banking.Web/Security/JwtTokenIssuer.cs` für echte Tokens verwendet. Die Api validiert sie unabhängig und unterscheidet nicht, woher ein Token stammt.

### Warum LocalDB statt Testcontainers

Die Vorgabe war "Verwende **nach Möglichkeit** Testcontainers für SQL Server". Auf dieser Entwicklungsmaschine ist **kein Docker installiert** (weder `docker`-CLI noch der Docker-Desktop-Dienst vorhanden) - Testcontainers benötigt zwingend einen laufenden Docker-Daemon, ist hier also nicht nutzbar (siehe [decisions-backlog.md](../adr/decisions-backlog.md) #22).

Statt ersatzweise auf EF Core InMemory oder SQLite auszuweichen (beide übersetzen SQL anders als echtes SQL Server und hätten z. B. die `ComplexProperty`-Zuordnung von `Money` oder Optimistic-Concurrency-Verhalten nicht realistisch geprüft), verwenden die Tests **LocalDB** - dieselbe echte SQL-Server-Engine, die auch für die lokale Entwicklung läuft. Jede `IntegrationTestWebApplicationFactory`-Instanz bekommt eine frisch angelegte, eindeutig benannte Datenbank (per `Database.MigrateAsync()` erzeugt, in `DisposeAsync()` wieder gelöscht) - das liefert dieselbe Isolation wie ein frischer Testcontainer (kein Zustand aus einem vorherigen Testlauf), nur ohne Container-Overhead und ohne Docker-Abhängigkeit.

### Aufbau

- `IntegrationTestWebApplicationFactory.cs` - die zentrale Factory: überschreibt Connection String + JWT-Konfiguration, migriert die frische Datenbank, sät einen Referenz-Kunden/-Konto, bietet `IssueJwt(params string[] roles)` zum Bauen von Test-Tokens.
- `IntegrationTestCollection.cs` - eine xUnit Collection Fixture, damit sich alle Testklassen **eine** Factory-Instanz (und Datenbank) teilen, statt pro Testklasse eine neue LocalDB-Datenbank anzulegen (spürbar langsamer). Schreibende Tests erzeugen deshalb bewusst eigene, eindeutige Datensätze statt sich auf einen globalen Zustand zu verlassen.
- `PaymentsApiTests.cs`, `CustomersApiTests.cs`, `TransactionsApiTests.cs` - je ein Test pro gefordertem Endpunkt und Dimension.

### Abgedeckte Endpunkte und Dimensionen

| Kapitel-17-Vorgabe | Endpunkt | Test |
|---|---|---|
| POST /api/payments | `PaymentsApiTests` | `Create_WithValidRequest_Returns201AndPersistsPayment` (Status Codes, Persistence, API Response), `Create_WithZeroAmount_Returns400WithProblemDetails` (Validation, Error Handling), `Create_ForUnknownSourceAccount_Returns404` (Error Handling), `Create_WithoutAuthentication_Returns401` (Status Codes) |
| GET /api/payments/{id} | `PaymentsApiTests` | über `Create_WithValidRequest_Returns201AndPersistsPayment` (folgt dem `Location`-Header und prüft die persistierten Daten), `GetById_ForNonExistentPayment_Returns404` |
| GET /api/customers | `CustomersApiTests` | `GetAll_ReturnsSeededCustomer` (Persistence, API Response), `GetAll_WithoutAuthentication_Returns401` |
| GET /api/transactions | `TransactionsApiTests` | `GetAll_ForSeededAccountWithNoTransactions_ReturnsOkWithEmptyPage` (API Response), `GetAll_ForUnknownAccount_Returns404` (Error Handling), `GetAll_WithoutAuthentication_Returns401` |

Damit sind alle fünf geforderten Dimensionen abgedeckt: **HTTP Status Codes** (200/201/400/401/404 jeweils mindestens einmal), **Validation** (Betrag 0 → 400), **Persistence** (Zahlung wird über einen zweiten, unabhängigen Request nachweislich wiedergefunden - nicht nur die erste Antwort geglaubt), **API Response** (Antwortform inkl. `Location`-Header, DTO-Felder), **Error Handling** (ProblemDetails-Struktur inkl. `code`-Feld, konsistent mit `ResultExtensions`).

### Eine Falle beim Konfigurieren von WebApplicationFactory-Tests

`Banking.Api/Program.cs` liest `ConnectionStrings:BankingDatabase` (in `AddInfrastructure`) und `Jwt:SigningKey`/`Issuer`/`Audience` **eager**, also noch bevor `builder.Build()` aufgerufen wird. `WebApplicationFactory.ConfigureWebHost(...).ConfigureAppConfiguration(...)` - der naheliegende Weg, Konfiguration für Tests zu überschreiben - greift aber erst am `Build()`-Interception-Punkt, also **zu spät** für Werte, die schon vorher gelesen wurden. Die Folge wäre nicht einmal ein Testfehler, sondern etwas Subtileres: die Tests würden unbemerkt gegen die echte lokale Entwickler-Datenbank laufen und mit dem echten Dev-JWT-Schlüssel validieren, während `IssueJwt(...)` mit dem Test-Schlüssel signiert - jeder authentifizierte Request würde mit 401 fehlschlagen, weil die Signatur nicht passt.

Die Lösung: `IntegrationTestWebApplicationFactory` setzt die Overrides stattdessen als **Umgebungsvariablen** (`Environment.SetEnvironmentVariable(...)` im Konstruktor, bevor `WebApplicationBuilder.CreateBuilder()` überhaupt läuft) - derselbe Mechanismus, der in diesem Projekt auch sonst überall zum lokalen Überschreiben von Configuration/Secrets verwendet wird (siehe [configuration.md](configuration.md)), nur programmatisch statt per PowerShell gesetzt.

### Eine zweite Falle: JSON-Deserialisierung im Testclient

`HttpContent.ReadFromJsonAsync<T>()`/`PostAsJsonAsync(...)` verwenden ohne explizite `JsonSerializerOptions` die bloßen `System.Text.Json`-Standardwerte (case-sensitiv, `PascalCase` erwartet). Die Api antwortet aber mit camelCase-Feldnamen, Enums als Klartext-Strings (`JsonStringEnumConverter`) und Strongly-Typed-IDs als reinem Guid-String (`StronglyTypedIdJsonConverterFactory`, siehe `Banking.Api/Program.cs`). Ohne passende Gegenstelle im Testclient würden Felder wie `TotalCount`/`Status`/`Id` beim Deserialisieren einfach leer/`default` bleiben oder eine `JsonException` werfen - kein offensichtlicher Fehler, sondern stille Fehlassertions.

`IntegrationTestJsonOptions.cs` spiegelt deshalb exakt die Optionen der Api (`JsonSerializerDefaults.Web` + `JsonStringEnumConverter` + `StronglyTypedIdJsonConverterFactory`, aus `Banking.Api.Serialization` wiederverwendet statt neu geschrieben) - dasselbe Prinzip wie `Banking.Web/Services/ApiJsonOptions.cs` für den echten Blazor-Client.

### Eine dritte Falle: der LocalDB-Alias

Beim ersten tatsächlichen Testlauf schlug die Migration mit einem SQL-Netzwerkfehler fehl ("SQLUserInstance.dll kann nicht geladen werden... ist keine zulässige Win32-Anwendung"), obwohl dieselbe Datenbank über die normale Anwendung (`dotnet run`) einwandfrei erreichbar ist. Der Alias `(localdb)\MSSQLLocalDB` versagt auf dieser Maschine wiederkehrend - ein bekannter, rein umgebungsbedingter LocalDB-Quirk (trat während der gesamten Projektlaufzeit auch beim manuellen lokalen Starten der Api gelegentlich auf). Zuverlässig funktioniert stattdessen die tatsächliche, aktuell aktive Named Pipe der Instanz.

`IntegrationTestWebApplicationFactory.ResolveLocalDbServer()` ruft deshalb `sqllocaldb info MSSQLLocalDB` zur Laufzeit auf und extrahiert die aktuelle Pipe-Adresse (`np:\\.\pipe\LOCALDB#<id>\tsql\query`) statt den Alias fest zu verdrahten - mit Rückfallebene auf den Alias, falls `sqllocaldb` nicht verfügbar ist (z. B. auf einer anderen Maschine oder in einer künftigen CI-Pipeline mit "echtem" SQL Server statt LocalDB).

### Status: lokal vollständig live verifiziert

```bash
sqllocaldb start MSSQLLocalDB
dotnet test tests/Banking.IntegrationTests/Banking.IntegrationTests.csproj
```

**Alle 10 Tests grün** (`Fehler: 0, erfolgreich: 10, gesamt: 10`, ca. 1 Sekunde) - inklusive echter Migration gegen eine frisch angelegte LocalDB-Datenbank, echtem Auf- und Abbau der Datenbank pro Testlauf, und allen fünf geforderten Dimensionen. Ebenso erneut bestätigt: `Banking.UnitTests` (28/28 grün) - beide Testprojekte liefen zuvor gegen McAfees Anwendungssteuerungsrichtlinie sowie einen LocalDB-Alias-Fehler (siehe unten), beides war ein reines Umgebungsproblem, kein Code-Problem, und ist inzwischen behoben bzw. hat sich von selbst gelöst.

### Unit Test vs. Integration Test vs. End-to-End Test

- **Unit Test** (Kapitel 16): testet **eine einzelne Klasse isoliert**, alle Abhängigkeiten gemockt. Beispiel: `CreatePaymentHandlerTests` - prüft die Entscheidungslogik des Handlers, ohne dass irgendetwas Echtes drumherum existiert (keine Datenbank, kein HTTP). Läuft in Millisekunden, zeigt bei einem Fehlschlag exakt, welche Klasse falsch ist. Beweist **nicht**, dass die echten Abhängigkeiten (Datenbank, HTTP-Pipeline) tatsächlich so funktionieren, wie es der Mock unterstellt.
- **Integration Test** (Kapitel 17): testet **mehrere echte Komponenten im Zusammenspiel**, typischerweise innerhalb eines Prozesses. Beispiel: `PaymentsApiTests` - die echte Api (Routing, Auth, Handler, EF Core) läuft gegen eine echte Datenbank, nur der Netzwerk-Layer wird übersprungen (`WebApplicationFactory` spricht In-Process mit der Api, kein echter Socket). Beweist, dass die Teile zueinander passen (z. B. dass die EF-Core-Konfiguration tatsächlich das speichert, was der Handler übergibt) - aber nie das komplette System von außen, wie es ein Nutzer erleben würde.
- **End-to-End Test**: testet das **komplette System aus Nutzersicht**, über echte Netzwerkgrenzen hinweg - z. B. ein Browser (Selenium/Playwright), der tatsächlich `Banking.Web` im Browser bedient, das wiederum echtes HTTP an eine separat laufende `Banking.Api`-Instanz mit echter, dauerhafter Datenbank schickt. Am langsamsten und am unzuverlässigsten (Browser-Timing, Netzwerk, mehrere unabhängig gestartete Prozesse), aber die einzige Ebene, die beweist, dass das System *als Ganzes* für einen echten Nutzer funktioniert - inklusive Dingen, die weder Unit- noch Integration Tests sehen können (JavaScript/SignalR-Verhalten im echten Browser, tatsächliche Netzwerk-Latenz, Zusammenspiel mehrerer eigenständiger Prozesse). In diesem Projekt (noch) nicht umgesetzt - wäre der nächste, deutlich aufwändigere Schritt nach den Integration Tests.

Die drei Ebenen ergänzen sich, sie ersetzen sich nicht: je "echter" die Ebene, desto mehr wird abgedeckt, aber auch desto langsamer und fehleranfälliger (durch Umgebungsfaktoren) wird der Test. Ein gutes Test-Portfolio hat **viele** Unit Tests, **weniger, aber gezielte** Integration Tests, und **wenige** End-to-End-Tests für die wichtigsten Nutzer-Abläufe - das ist die klassische "Test-Pyramide".
