# Transaktions-Import (Azure Data Factory, Kapitel 12)

## Warum keine echte Azure Data Factory

Der ursprüngliche Plan ([Kapitel 12 im Anforderungsdokument](../prompt/ProjektErstellung.docx)) beschreibt ein Szenario, bei dem ein externes System täglich eine CSV-Datei mit Transaktionen liefert, die über die Pipeline **Blob Storage → Azure Data Factory → Validation → Transformation → Azure SQL → Banking Portal** verarbeitet wird.

Azure Data Factory ist ein reiner Cloud-Dienst - ohne Azure-Subscription (siehe [decisions-backlog.md](../adr/decisions-backlog.md) #17) lässt er sich nicht real betreiben. Statt die Pipeline nur zu behaupten, bilden wir die **fachlichen Pipeline-Schritte** (Validation, Transformation) als echten, kompilierten und lokal lauffähigen Code nach - nur die eigentliche ADF-Orchestrierung wird simuliert (ein Datei-Upload statt eines Blob-Storage-Events).

## Die Pipeline Schritt für Schritt

| Schritt (Original) | Umsetzung in diesem Projekt |
|---|---|
| **Blob Storage** | Ein externes System würde die Datei in einen Blob-Container legen. Hier: ein Datei-Upload über `POST /api/imports` (Blazor: `/imports`-Seite) löst denselben Vorgang direkt aus. |
| **Azure Data Factory (Trigger/Orchestrierung)** | ADF würde auf ein "neue Datei"-Event reagieren und die Pipeline starten. Hier: der Upload-Request selbst startet die Verarbeitung synchron - kein separater Trigger-Mechanismus nötig. |
| **Validation** | `ImportTransactionsCsvHandler.TryValidate(...)`: rein formale Prüfung jeder CSV-Zeile (Pflichtfelder, gültige Guid/Zahl/Datum/Währung) - noch ohne Datenbankzugriff. Fehlerhafte Zeilen werden übersprungen, nicht der ganze Lauf abgebrochen. |
| **Transformation** | Auflösen der `AccountNumber` zu einem echten Konto, Abgleich `CustomerId` gegen den tatsächlichen Kontoinhaber, Abgleich der Währung, und die eigentliche Übersetzung: CSV erlaubt negative Beträge (Soll-/Habenkennzeichen), unser `Money`-Wertobjekt ist immer positiv - das Vorzeichen wird zum `TransactionType` (Deposit/Withdrawal). |
| **Azure SQL** | Ganz normal über EF Core / `IUnitOfWork`, wie überall sonst im Projekt - kein Unterschied zu jeder anderen Schreiboperation. |
| **Banking Portal** | Die Blazor-Seite `/imports` zeigt jeden Lauf mit Status und Kennzahlen; `/imports/{id}` zeigt das Ergebnis jeder einzelnen Zeile. |

## Datenmodell

`ImportJob` (Aggregat) mit Kind-Entity `ImportRecord` - das erste Aggregat mit echter Kind-Collection in diesem Projekt:

- **ImportJob**: `FileName`, `StartedAt`, `CompletedAt`, `Status` (`Started` → `Processing` → `Completed`/`Failed`), sowie `SuccessfulRecords`/`FailedRecords`/`SkippedRecords` (`TotalRecords` ist die Summe, kein eigenes Feld).
- **ImportRecord**: pro CSV-Zeile ein Ergebnis - `RowNumber`, `Outcome` (`Success`/`Failed`/`Skipped`), `ExternalTransactionId` (die `TransactionId`-Spalte aus der CSV), `ErrorMessage` bei Fehlern.

Ein Lauf, bei dem **jede** Zeile fehlschlägt, gilt als `Failed` (typischerweise ein grundsätzliches Problem wie ein falsches Dateiformat). Ein Lauf mit teilweise fehlerhaften Zeilen gilt als `Completed` - das entspricht dem Verhalten realer ETL-Pipelines, die einzelne ungültige Datensätze überspringen, statt den ganzen Lauf abzubrechen.

## Idempotenz

Jede CSV-Zeile trägt eine externe `TransactionId` (vom liefernden System vergeben). Bevor eine Zeile verarbeitet wird, prüft `IImportJobRepository.ExistsSuccessfulRecordAsync(...)`, ob diese `TransactionId` bereits in einem **früheren** Lauf erfolgreich verarbeitet wurde - dann wird die Zeile als `Skipped` markiert statt die Transaktion doppelt zu buchen. Das ist ein zentrales Merkmal echter ETL-Pipelines: Sowohl ein versehentlich zweimal hochgeladenes File als auch ein von Azure Data Factory automatisch wiederholter Pipeline-Lauf nach einem Teilfehler dürfen keine doppelten Buchungen erzeugen.

## CSV-Format

Dateiname: `transactions_YYYY_MM_DD.csv`

| Spalte | Beispiel | Bedeutung |
|---|---|---|
| `TransactionId` | `EXT-2026-08-31-0001` | Externe, eindeutige Id des liefernden Systems (Grundlage der Idempotenz-Prüfung) |
| `CustomerId` | `11111111-1111-1111-1111-111111111111` | Guid des Kunden - wird gegen den tatsächlichen Kontoinhaber geprüft |
| `AccountNumber` | `DE89370400440532013000` | Muss ein existierendes Konto sein |
| `Amount` | `-42.50` | Negativ = Withdrawal, positiv = Deposit (siehe Transformation oben) |
| `Currency` | `EUR` | Muss zur Kontowährung passen |
| `BookingDate` | `2026-08-31` | Darf nicht in der Zukunft liegen |
| `Description` | `Miete August` | Optional, maximal 500 Zeichen |

Beispieldatei:

```csv
TransactionId,CustomerId,AccountNumber,Amount,Currency,BookingDate,Description
EXT-2026-08-31-0001,11111111-1111-1111-1111-111111111111,DE89370400440532013000,-42.50,EUR,2026-08-31,Miete August
EXT-2026-08-31-0002,11111111-1111-1111-1111-111111111111,DE89370400440532013000,1000.00,EUR,2026-08-31,Gehaltseingang
```

## So nutzt du den Import (README)

**Voraussetzung**: Rolle `OperationsManager` oder `Administrator` zum Auslösen eines Imports; `BankEmployee` genügt zum Ansehen (siehe [Rollenmodell](../../README.md)).

1. **Über die Weboberfläche**: `/imports` öffnen, im Upload-Bereich eine CSV-Datei im obigen Format auswählen. Der Lauf erscheint sofort in der Tabelle mit Status und Kennzahlen; ein Klick auf "Details" zeigt das Ergebnis jeder Zeile inklusive Fehlermeldungen.
2. **Über die Api direkt** (z. B. zum Testen):
   ```bash
   curl -X POST http://localhost:5080/api/imports \
     -H "Authorization: Bearer <JWT mit OperationsManager-Rolle>" \
     -F "file=@transactions_2026_08_31.csv"
   ```
3. **Status prüfen**: `GET /api/imports` (Liste) bzw. `GET /api/imports/{id}` (Details mit Zeilenergebnissen).

## Bewusste Vereinfachung: synchrone statt asynchrone Verarbeitung

Eine echte Azure-Data-Factory-Pipeline läuft asynchron (Trigger → Queue → Worker). Für den Umfang dieses Portfolio-Projekts verarbeitet `ImportTransactionsCsvHandler` die Datei synchron innerhalb des Upload-Requests - ohne Hintergrund-Job/Queue-Infrastruktur (z. B. Hangfire), die für den restlichen Funktionsumfang der Anwendung nicht gebraucht wird. Das Datenmodell (`ImportJob` mit Status-Übergängen) ist bewusst trotzdem so gebaut, dass eine spätere Umstellung auf echte asynchrone Verarbeitung keine Änderung an Domain oder Api-Vertrag erfordern würde - nur `ImportTransactionsCsvHandler` würde dann von einem Hintergrund-Worker statt direkt vom Controller aufgerufen.

## Status: lokal vollständig live verifiziert

Migration `AddImportJobs` erfolgreich gegen LocalDB angewendet:

```bash
dotnet ef database update --project src/Banking.Infrastructure --startup-project src/Banking.Api
```

Anschließend end-to-end gegen die laufende Api getestet (`POST`/`GET /api/imports`):

- Eine Testdatei mit 5 Zeilen (2 gültig, 1 mit falscher `CustomerId`/Konto-Zuordnung, 1 mit unbekanntem Konto, 1 mit ungültigem Betrag) ergab korrekt `successfulRecords: 2`, `failedRecords: 3`, `status: Completed`, mit nachvollziehbaren Fehlermeldungen pro Zeile.
- Die beiden erfolgreichen Zeilen erzeugten echte, gebuchte `Transaction`-Einträge auf dem Konto - inklusive korrekt aus dem CSV-Vorzeichen abgeleitetem `TransactionType` (negativer Betrag → `Withdrawal`, positiver → `Deposit`).
- **Idempotenz bestätigt**: derselbe Upload ein zweites Mal ergab `successfulRecords: 0`, `skippedRecords: 2` (die beiden bereits importierten Zeilen), `failedRecords: 3` (dieselben ungültigen Zeilen scheitern erneut, wie erwartet) - keine doppelten Buchungen.
- **Rollenmodell bestätigt**: `POST /api/imports` mit einem reinen `BankEmployee`-Token liefert `403`; `GET /api/imports` mit demselben Token liefert `200`.

(Hinweis am Rande: Bei der Verifikation zeigte `curl | python3 -m json.tool` die Fehlermeldungen zunächst mit falsch dargestellten Umlauten - das lag an Pythons Standard-Zeichenkodierung beim Lesen von Pipe-Eingaben unter Windows, nicht an der Anwendung. Ein direkter Blick auf die rohen Response-Bytes bestätigte korrektes UTF-8.)

## Wie ich das im Bewerbungsgespräch erklären würde

> "Wir simulieren eine Azure-Data-Factory-Pipeline, ohne Azure zu brauchen: Der fachliche Kern einer solchen Pipeline sind die Schritte Validation und Transformation - genau die haben wir als echten, getesteten Code gebaut, nur die Cloud-Orchestrierung drumherum haben wir durch einen einfachen Datei-Upload ersetzt. Jeder Lauf wird als eigenes Aggregat (`ImportJob`) mit Status und einer Kind-Collection von Zeilenergebnissen (`ImportRecord`) persistiert - das ist unser 'Monitoring', ganz ohne Azure Monitor.
>
> Der Punkt, den ich am spannendsten finde, ist Idempotenz: ETL-Pipelines laufen in der Praxis nicht immer genau einmal - eine Datei kann versehentlich zweimal ankommen, oder eine Pipeline wiederholt sich nach einem Teilfehler automatisch. Wir erkennen bereits verarbeitete Zeilen über ihre externe TransactionId und überspringen sie, statt doppelt zu buchen. Und wenn einzelne Zeilen fehlerhaft sind - falsches Format, unbekanntes Konto, falscher Kunde -, bricht das nicht den ganzen Lauf ab, sondern wird pro Zeile festgehalten, genau wie eine echte ADF-Data-Flow-Validation das auch tun würde."

Mögliche Interviewer-Rückfragen:

- **"Warum synchron statt über eine Queue?"** → Für den Projektumfang ausreichend; das Datenmodell ist so gebaut, dass eine spätere Umstellung auf einen Hintergrund-Worker keine Domain-/Api-Änderung erfordern würde.
- **"Was passiert bei einer riesigen Datei?"** → `RequestSizeLimit` auf der Api begrenzt den Upload (aktuell 10 MB) - für eine wirklich große tägliche Lieferung bräuchte man ohnehin die asynchrone, Queue-basierte Variante statt eines synchronen Requests.
- **"Wie verhindert ihr doppelte Buchungen bei einem Retry?"** → Idempotenz über die externe TransactionId, siehe oben.
