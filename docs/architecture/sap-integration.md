# SAP-Integration

## Warum kein echter SAP .NET Connector (NCo)

Der ursprüngliche Plan ([Kapitel 11 im Anforderungsdokument](../prompt/ProjektErstellung.docx)) nennt SAP-RFC-Integration. Der Chapter-Text selbst sieht bereits vor: *"Falls eine echte SAP-RFC-Library nicht verfügbar ist, implementiere eine realistische Abstraktion und Mock-Integration statt eine nicht funktionierende SAP-Verbindung zu erfinden."* Genau das setzen wir um - aus einem konkreten Grund:

Klassisches SAP-RFC/BAPI läuft über den **SAP .NET Connector (NCo)** - eine proprietäre Bibliothek, die es nicht auf NuGet gibt und die nur mit aktivem SAP-Vertrag/-Account von SAP selbst bezogen werden kann. Ohne SAP-Zugangsdaten (die dieses Portfolio-Projekt nicht hat) lässt sich damit weder etwas kompilieren noch testen.

Viele reale .NET-zu-SAP-Integrationen laufen in der Praxis ohnehin nicht über direktes RFC vom Applikationsserver aus, sondern über ein **RFC-fähiges SAP-Gateway/OData-Service**, das SAP dafür exponiert. Genau dieses realistischere Muster bilden wir über einen ganz normalen `HttpClient` ab - dadurch ist der Code tatsächlich kompilier- und lokal testbar, ohne die Architektur (Anti-Corruption Layer, ADR #9/#19) zu verwässern.

## Architektur

```
Banking.Application (Domain-nah, kennt kein SAP)
  ISapCustomerService
      GetCustomerAsync(CustomerNumber)  -> SapCustomerProfile?
      GetAccountAsync(AccountNumber)    -> SapAccountInfo?

Banking.Infrastructure (kennt SAP-Details)
  MockSapCustomerService     - Standard lokal, deterministische Fake-Daten
  SapRfcCustomerService      - HttpClient-basiert, mit Resilience-Handler
  SapCustomerResponse/
  SapAccountResponse         - rohes SAP-Antwortformat (BAPI-artige Feldnamen), nur intern bekannt
```

Domain und Application kennen ausschließlich `ISapCustomerService` und die eigenen Typen `SapCustomerProfile`/`SapAccountInfo` - nie SAP-spezifische Feldnamen oder Formate. Das Mapping SAP-Modell → internes Modell passiert explizit und ausschließlich in `SapRfcCustomerService`, direkt an der Systemgrenze.

## Gate-Muster: Mock vs. echte Anbindung

Identisch zu Key Vault/App Configuration (Kapitel 9) und Application Insights (Kapitel 10): `InfrastructureServiceCollectionExtensions.AddInfrastructure` prüft `Sap:BaseUrl`.

- **Nicht konfiguriert (Standardfall, kein SAP-System vorhanden):** `MockSapCustomerService` wird registriert. Liefert deterministische Fake-Daten (Adresse, Steuernummer, Risikoeinstufung, Konto-Status) - abgeleitet aus einem stabilen Hash der Kunden-/Kontonummer, damit derselbe Kunde bei jedem Aufruf (auch über Neustarts hinweg) dieselben Werte liefert. Simuliert außerdem realistische Latenz und - für ca. jeden siebten Schlüssel, reproduzierbar - einen SAP-Ausfall.
- **Konfiguriert:** `SapRfcCustomerService` wird stattdessen per `AddHttpClient<ISapCustomerService, SapRfcCustomerService>()` registriert, mit `AddStandardResilienceHandler()` (`Microsoft.Extensions.Http.Resilience`) für Timeout, Retry und Circuit Breaker - Cross-Cutting-Concerns, die nicht in der Klasse selbst, sondern deklarativ im Handler konfiguriert sind.

## Berücksichtigte Aspekte (aus dem Anforderungstext)

| Aspekt | Umsetzung |
|---|---|
| Timeout | `SapOptions.RequestTimeout` → `resilience.AttemptTimeout` |
| Retry | `SapOptions.MaxRetryAttempts` → `resilience.Retry.MaxRetryAttempts` |
| Fehlerbehandlung | Jede technische Ausnahme (HTTP-Fehler, Timeout/Retry-Erschöpfung, fehlerhaftes JSON) wird an der Systemgrenze in eine einzige `SapIntegrationException` übersetzt - der Rest der Anwendung kennt nur diesen einen Fehlertyp, nie SAP-/Polly-spezifische Exceptions |
| Logging | Strukturierte `LogWarning` beim SAP-Ausfall (kein `LogError` - eine nicht erreichbare externe Abhängigkeit ist ein erwarteter Betriebsfall, keine Bug-Situation) |
| CancellationToken | Wird durchgereicht; eine echte Cancellation durch den Aufrufer (`OperationCanceledException` bei bereits angefordertem Abbruch) wird explizit *nicht* in `SapIntegrationException` verpackt, sondern unverändert weitergereicht |
| Externe Abhängigkeit | Siehe Graceful-Degradation-Abschnitt unten - ein SAP-Ausfall lässt die eigentliche Anfrage nicht scheitern |
| Mapping SAP-Modell ↔ internes Modell | `SapCustomerResponse`/`SapAccountResponse` (BAPI-artige Feldnamen wie `StCd1`, `Stras`, `Land1`) sind nur `SapRfcCustomerService` bekannt; die Übersetzung in `SapCustomerProfile`/`SapAccountInfo` passiert explizit in dieser einen Klasse |

## Graceful Degradation statt Komplettausfall

`GetCustomerSapProfileHandler` (Anwendungsfall: "Customer 360"-Ansicht, `GET /api/customers/{id}/sap-profile`) fängt `SapIntegrationException` gezielt ab: schlägt die SAP-Anreicherung fehl, liefert der Endpunkt trotzdem **200 OK** mit den rein lokalen Daten und `sapAvailable: false`, statt die komplette Anfrage mit 500 scheitern zu lassen. Eine reine Datenanreicherung aus einem Drittsystem darf nicht die Verfügbarkeit der eigentlichen Kernfunktion (Kundendaten ansehen) gefährden.

```json
{
  "customerNumber": "C-1001",
  "sapAvailable": false,
  "sapProfile": null,
  "accounts": [{ "accountNumber": "...", "sapAccountStatus": null }]
}
```

## Lokal verifiziert

- **Happy Path (Mock, Standardkonfiguration):** `GET /api/customers/{id}/sap-profile` liefert lokale Kundendaten + SAP-Anreicherung (Adresse, Steuernummer, Risikoeinstufung, Konto-Status) mit `sapAvailable: true`.
- **SAP nicht erreichbar (echte `SapRfcCustomerService`-Implementierung, `Sap:BaseUrl` auf einen absichtlich nicht lauschenden Port gesetzt):** Der Resilience-Handler griff nachweislich (Retry, danach `Polly.Timeout.TimeoutRejectedException`), der Handler loggte eine `LogWarning` ("SAP nicht erreichbar...") und lieferte trotzdem `200 OK` mit `sapAvailable: false` statt eines 500ers - der Graceful-Degradation-Pfad funktioniert also nicht nur gegen den Mock, sondern auch gegen die echte HttpClient-Implementierung.
- **404-Fall:** Eine nicht existierende Kunden-Id liefert weiterhin korrekt 404, unabhängig von SAP.

## Wie ich das im Bewerbungsgespräch erklären würde

> "SAP ist bei uns über einen klassischen Adapter/Anti-Corruption-Layer angebunden: Domain und Application kennen nur ein Interface, `ISapCustomerService`, mit eigenen, von SAP unabhängigen DTOs. Die tatsächliche Anbindung - egal ob RFC, ein SAP-Gateway oder später mal ein ganz anderes System - lebt komplett in der Infrastructure-Schicht und kann ausgetauscht werden, ohne dass Domain oder Application etwas davon merken.
>
> Weil ein echter SAP .NET Connector eine lizenzpflichtige Bibliothek ist, die man nicht einfach installieren kann, habe ich die reale Implementierung als HttpClient gegen ein RFC-fähiges SAP-Gateway gebaut - das ist ohnehin ein verbreitetes Integrationsmuster in der Praxis, wenn man nicht direkt vom Applikationsserver aus RFC sprechen will oder kann. Timeout, Retry und Circuit Breaker kommen über das offizielle Microsoft-Resilience-Paket, nicht handgeschrieben.
>
> Der Punkt, auf den ich am meisten Wert gelegt habe, ist die Fehlerbehandlung: SAP ist eine externe Abhängigkeit außerhalb unserer Kontrolle. Wenn SAP down ist, will ich nicht, dass unsere komplette Kundenansicht mit einem 500er ausfällt, nur weil eine Zusatzinformation fehlt - stattdessen liefere ich die lokalen Daten trotzdem aus und markiere transparent, dass die SAP-Anreicherung gerade nicht verfügbar war. Das habe ich auch tatsächlich gegen einen absichtlich nicht erreichbaren Endpunkt getestet, nicht nur gegen den Mock."

Mögliche Interviewer-Rückfragen und Antworten:

- **"Warum kein echtes SAP RFC?"** → Proprietäre, lizenzpflichtige Bibliothek (SAP NCo), nicht ohne SAP-Vertrag beziehbar/testbar; HttpClient gegen ein SAP-Gateway ist ein in der Praxis verbreitetes, gleichwertiges Integrationsmuster.
- **"Was passiert, wenn SAP eine Zahlung ablehnt/braucht?"** → Diese Anbindung ist rein lesend (Kundenstammdaten-Anreicherung). Ein schreibender Anwendungsfall (z. B. Zahlungsfreigabe an SAP melden) würde denselben Adapter-Ansatz nutzen, bräuchte aber zusätzlich Idempotenz (z. B. Idempotency-Key), da Retries bei einem Schreibvorgang doppelte Effekte verursachen könnten - bei einer reinen Leseoperation wie hier ist das unkritisch.
- **"Wie verhindert ihr, dass ein SAP-Ausfall die ganze Anwendung lahmlegt?"** → Graceful Degradation im Handler (siehe oben) plus Circuit Breaker im Resilience-Handler, der bei wiederholten Ausfällen weitere Anfragen kurzfristig gar nicht erst an SAP schickt.
- **"Warum ein eigener Exception-Typ (`SapIntegrationException`)?"** → Anti-Corruption Layer: der Rest der Anwendung soll nie wissen müssen, ob SAP per RFC, HTTP oder sonst etwas angebunden ist, und auch nicht mit Polly- oder HTTP-spezifischen Exception-Typen umgehen müssen.
