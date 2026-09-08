# Unit Tests (Kapitel 16)

## Werkzeuge

- **xUnit** - war bereits als Testframework eingerichtet (Central Package Management), nur mit der leeren Standard-Vorlage.
- **NSubstitute** (neu) - für Mocks der Repository-Interfaces. Bewusst **nur externe Abhängigkeiten** gemockt (Datenbank-Zugriffe über die Repository-Interfaces) - FluentValidation-Validatoren sind reine, schnelle In-Process-Logik und werden **echt** ausgeführt, nicht gemockt. Das ist genauer als ein reiner Handler-Test mit immer-gültigem Validator-Mock, weil es echte FluentValidation-Regeln (z. B. das Zusammenspiel mehrerer Regeln) mitprüft.

Alle Tests laufen ohne Datenbank, ohne Netzwerk, ohne laufende Anwendung - **28 Tests in unter einer Sekunde** (`dotnet test`), wie von Kapitel 16 gefordert ("schnell und deterministisch").

## Aufbau: drei Test-Ebenen für "Payment Processing"

| Ebene | Datei | Was getestet wird | Mocking |
|---|---|---|---|
| Domain | `tests/Banking.UnitTests/Domain/Payments/PaymentTests.cs` | Statusübergänge des `Payment`-Aggregats (State Machine), `Payment.Create`-Guards | keins |
| Application (Validator) | `tests/Banking.UnitTests/Payments/CreatePaymentValidatorTests.cs` | FluentValidation-Regeln (Betrag ≤ 0, Betrag über Limit, Pflichtfelder) | keins |
| Application (Handler) | `tests/Banking.UnitTests/Payments/CreatePaymentHandlerTests.cs` | Geschäftsregeln, die einen Datenbankzugriff brauchen (Konto/Kunde laden, Kontostand, Duplikat-Prüfung) | Repositories + Validator gemockt |

## Vorher fehlende Business Rules: jetzt implementiert

Beim Abgleich der von Kapitel 16 geforderten Testfälle mit dem bestehenden Code fiel auf, dass drei Regeln schlicht noch nicht existierten - sie wurden ergänzt, bevor sie getestet werden konnten (siehe [decisions-backlog.md](../adr/decisions-backlog.md) #21):

- **Inaktiver Kunde**: `CreatePaymentHandler` prüfte bisher nur den Kontostatus, nie den Status des zugehörigen Kunden. Ein Konto kann aktiv sein, während der Kunde nachträglich gesperrt wurde.
- **Unzureichender Kontostand**: Der eigentliche Kontostand-Abzug passiert erst bei der Ausführung einer Zahlung (`Account.Debit`, noch nicht implementiert) - trotzdem sollte ein offensichtlich ungedeckter Auftrag nicht einmal angelegt werden können. Kreditkonten sind bewusst ausgenommen (dürfen laut Domain-Modell ins Minus laufen).
- **Doppelte Zahlung**: Schutz vor versehentlicher Doppel-Einreichung (z. B. Doppelklick) über ein kurzes Zeitfenster (1 Minute) auf exakt gleiche Eckdaten (Quellkonto, Zielkonto, Betrag, Währung).

Alle drei live gegen die laufende Anwendung verifiziert (nicht nur über Unit Tests): `Payment.InsufficientFunds`, `Payment.Duplicate` und `Customer.NotActive` liefern jeweils den erwarteten HTTP 409 mit nachvollziehbarer Fehlermeldung.

## Welche Business Rules getestet werden

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

## Welche Business Rules NICHT getestet werden

- **Alles, was eine echte Datenbank oder einen echten HTTP-Request braucht**: dass die EF-Core-Konfiguration korrekt ist (Spalten, Indizes, Fremdschlüssel), dass eine Migration tatsächlich anwendbar ist, dass das Optimistic-Concurrency-Verhalten (`RowVersion`) bei echten parallelen Schreibzugriffen greift. Das ist explizit Aufgabe der **Integration Tests** (Kapitel 17), nicht der Unit Tests.
- **Die Freigabe-Workflow-Endpunkte selbst** (`ApprovePaymentHandler`, `RejectPaymentHandler`) - noch ohne eigene Unit Tests, da sie strukturell sehr ähnlich zu `CreatePaymentHandler` sind (kein neues Testmuster nötig) und aus Zeitgründen für dieses Kapitel zurückgestellt wurden.
- **Andere Aggregate** (`Account`, `Customer`, `Transaction`, `ImportJob`) - Kapitel 16 verlangt explizit "die wichtigsten Business Rules, insbesondere Payment Processing"; die anderen Aggregate haben strukturell ähnliche State-Machine-Guards, die nach demselben Muster ergänzt werden können, sobald sie an der Reihe sind.
- **Nebenläufigkeit** (zwei gleichzeitige Freigaben desselben Zahlungsauftrags) - dafür bräuchte es entweder Integration Tests gegen eine echte Datenbank oder gezielte Concurrency-Tests, die über den Rahmen eines einfachen Unit Tests hinausgehen.
- **Die Duplikat-Erkennung als tatsächliche SQL-Abfrage** - der Unit Test prüft nur, dass der Handler bei `ExistsSimilarRecentAsync() == true` korrekt ablehnt; dass die SQL-Abfrage selbst (Übersetzung des LINQ-Ausdrucks durch EF Core, insbesondere der Vergleich der `ComplexProperty` `Money`) tatsächlich das Richtige tut, wurde nur live gegen LocalDB geprüft (siehe oben), nicht durch einen automatisierten Test.

## Warum Unit Tests die Integration Tests nicht ersetzen

Ein Unit Test wie in diesem Kapitel beweist: *"Wenn das Konto inaktiv ist, gibt der Handler `Account.NotActive` zurück"* - unter der Annahme, dass `IAccountRepository.GetByIdAsync` sich so verhält, wie es das Mock vorgibt. Er beweist **nicht**:

- dass `AccountRepository` (die echte EF-Core-Implementierung) tatsächlich das richtige Konto aus der echten Datenbank lädt,
- dass die HTTP-Pipeline (Routing, `[Authorize(Roles=...)]`, Model Binding, JSON-Serialisierung) den Request überhaupt bis zum Handler durchlässt,
- dass mehrere Schichten zusammenpassen (z. B. dass eine Änderung an der EF-Core-Konfiguration nicht versehentlich ein Feld falsch mappt, das der Unit Test gar nicht anfasst, weil er die Datenbank komplett durch ein Mock ersetzt),
- dass die Migration, die eine neue Spalte/Tabelle anlegt, überhaupt fehlerfrei gegen eine echte SQL-Server-Instanz läuft.

Unit Tests sind **schnell und isolieren die Fehlerursache** (schlägt ein Test fehl, weiß man sofort: die Logik in genau dieser einen Klasse ist falsch). Integration Tests sind **langsamer, aber realistischer** (echte Datenbank oder zumindest `WebApplicationFactory`) und decken genau die Lücken ab, die beim Mocken der Repositories entstehen. Beide zusammen ergeben erst ein vollständiges Sicherheitsnetz - das ist auch der Grund, warum Kapitel 17 (Integration Tests) im Anforderungsplan direkt auf dieses Kapitel folgt.
