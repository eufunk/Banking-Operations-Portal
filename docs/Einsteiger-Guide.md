# Einsteiger-Guide: Banking Operations Portal

*Eine Erklärung für alle, die zum ersten Mal auf dieses Projekt stoßen und weder Bank-Software noch die verwendeten Technologien kennen müssen, um zu verstehen, worum es hier geht.*

---

## 1. Was ist das für ein Projekt?

Das **Banking Operations Portal** ist eine Software, wie sie Bankangestellte im Innendienst benutzen würden - nicht die App, die man als Bankkunde auf dem Handy hat, sondern das Werkzeug dahinter, mit dem Mitarbeiter:

- Kundendaten nachschlagen,
- Konten einsehen,
- Kontobewegungen (Transaktionen) durchsuchen,
- Zahlungsaufträge anlegen und freigeben,
- und Massendaten-Importe (z. B. eine tägliche Lieferung von Transaktionsdaten) überwachen.

Es ist eine **Webanwendung**: Man öffnet sie im Browser wie eine ganz normale Internetseite, meldet sich mit Benutzername/Passwort an und sieht danach je nach Rolle unterschiedliche Funktionen.

**Wichtig zu wissen**: Es ist **kein echtes Bankensystem**. Es verarbeitet keine echten Kundendaten, ist an kein echtes Geldsystem angeschlossen und läuft nur auf dem eigenen Rechner (lokal), nicht im Internet. Es tut aber so, als wäre es eines - mit denselben Bausteinen, Mustern und Problemen, die auch in einer echten Bank-IT-Abteilung auftauchen würden.

## 2. Wofür wurde es gebaut?

Dieses Projekt ist ein **Portfolio-Projekt**: Es wurde gebaut, um sich für Stellen als .NET-/Azure-Softwareentwickler zu bewerben und im Bewerbungsgespräch etwas Konkretes zeigen und erklären zu können.

Der Zweck ist also nicht "eine Bank-App verkaufen", sondern:

1. **Lernen durch Bauen**: Statt nur Tutorials zu lesen, wurde ein realistisch großes, zusammenhängendes System gebaut, an dem echte Entscheidungen getroffen werden mussten (welche Datenbankstruktur, wie Fehler behandeln, wie Sicherheit umsetzen, ...).
2. **Enterprise-Realismus simulieren**: Banken-IT gilt als anspruchsvolles, stark reguliertes Umfeld mit hohen Ansprüchen an Sicherheit, Nachvollziehbarkeit und Architektur. Das Projekt bildet typische Bausteine einer solchen Umgebung nach: Rollen und Berechtigungen, Audit-Fähigkeit, Cloud-Anbindung (Azure), Anbindung an ein Altsystem (SAP), Datenimporte, Observability (Überwachbarkeit im Betrieb).
3. **Ehrlich dokumentierte Grenzen zeigen**: Für viele der "echten" Enterprise-Bausteine (Azure-Cloud-Dienste, ein SAP-System) fehlt im Rahmen eines privaten Portfolio-Projekts der Zugang (das kostet Geld bzw. erfordert einen Firmenvertrag). Statt das zu verschweigen, ist jede solche Stelle bewusst als **realistische, lokal lauffähige Alternative** gebaut und **transparent dokumentiert** - das ist selbst eine Fähigkeit, die in einem Bewerbungsgespräch zählt: erkennen, was fehlt, und einen sinnvollen Ersatz bauen, statt einfach zu behaupten, etwas würde funktionieren.

## 3. Wie wurde es erarbeitet?

Das Projekt folgt einem **vorbereiteten Anforderungsdokument** ([`docs/prompt/ProjektErstellung.docx`](prompt/ProjektErstellung.docx)) mit 24 Kapiteln - von "Projekt initialisieren" über "Domain Model", "REST API", "Authentifizierung" bis hin zu "Security Review" und einer abschließenden "Interview-Simulation". Jedes Kapitel beschreibt eine Aufgabe, wie sie in einem echten Enterprise-Projekt als Arbeitspaket vorkommen könnte.

Die eigentliche Umsetzung geschah **im Dialog mit Claude Code** (einem KI-Coding-Assistenten von Anthropic), kapitelweise:

1. Ein Kapitel wird besprochen und die Anforderung erklärt.
2. Vor größeren Architekturentscheidungen wird die Begründung kurz erläutert - inklusive Alternativen und was eine schlechte Lösung wäre.
3. Der Code wird geschrieben, gebaut (`dotnet build`) und wo möglich **live ausprobiert** (z. B. über curl-Aufrufe an die laufende Anwendung), nicht nur "sollte funktionieren".
4. Abweichungen vom ursprünglichen Plan (z. B. weil ein kommerzielles Werkzeug oder ein Azure-Zugang fehlt) werden **an drei Stellen** transparent festgehalten: direkt im betroffenen Kapitel des Anforderungsdokuments, als nummerierter Eintrag im [Entscheidungsprotokoll](adr/decisions-backlog.md), und wo nötig in einem eigenen technischen Dokument unter `docs/architecture/`.
5. Erst nach ausdrücklicher Bestätigung wird der Code per Git committet und auf GitHub veröffentlicht.

Der aktuelle Umsetzungsstand (was fertig ist, was noch fehlt) wird laufend in [`Fortschritt.md`](../Fortschritt.md) festgehalten.

## 4. Mit welchen Werkzeugen, Sprachen und Technologien wurde es gebaut?

Kurz erklärt, ohne Vorwissen vorauszusetzen:

| Werkzeug/Technologie | Was es ist | Wofür es hier verwendet wird |
|---|---|---|
| **.NET 10** | Eine von Microsoft entwickelte Plattform zum Bauen von Software | Das technische Fundament der gesamten Anwendung |
| **C#** | Die Programmiersprache | In ihr ist praktisch der gesamte Code geschrieben |
| **ASP.NET Core** | Der Teil von .NET für Webanwendungen | Trägt sowohl die REST-API als auch die Weboberfläche |
| **REST-API** | Eine Schnittstelle, über die Programme (nicht Menschen) miteinander sprechen, meist über das gleiche Protokoll wie ein Browser (HTTP) | Die zentrale Schnittstelle, über die die Weboberfläche mit dem "Gehirn" der Anwendung spricht |
| **Blazor Server** | Microsofts Technologie, um Weboberflächen mit C# statt JavaScript zu bauen | Die Weboberfläche, die Mitarbeiter im Browser sehen |
| **Entity Framework Core (EF Core)** | Ein Werkzeug, das automatisch zwischen C#-Code und Datenbank-Tabellen übersetzt | Speichert und liest alle Daten (Kunden, Konten, Zahlungen, ...) |
| **SQL Server / LocalDB** | Ein Datenbanksystem von Microsoft; LocalDB ist die kostenlose Variante für den eigenen Rechner | Die Datenbank, in der alle Daten liegen |
| **JWT (JSON Web Token) & Cookies** | Standardmechanismen, um zu beweisen "ich bin angemeldet" bzw. "ich bin Nutzer X mit Rolle Y" | Anmeldung und Absicherung der API-Aufrufe |
| **OpenTelemetry** | Ein herstellerneutraler Standard, um zu beobachten, was eine laufende Anwendung gerade tut (welche Aufrufe, wie lange, welche Fehler) | Nachvollziehbarkeit im Betrieb (siehe Abschnitt 6) |
| **CsvHelper** | Eine Programmbibliothek zum zuverlässigen Lesen von CSV-Dateien (Tabellen als Textdatei) | Für den täglichen Transaktions-Import |
| **Git & GitHub** | Ein Versionsverwaltungssystem bzw. eine Plattform dafür | Jede Änderung wird nachvollziehbar in kleinen, beschrifteten Schritten (Commits) festgehalten, das gesamte Projekt liegt öffentlich einsehbar auf GitHub |
| **Azure** (Key Vault, App Configuration, Application Insights, Data Factory, SQL, ...) | Microsofts Cloud-Plattform mit vielen einzelnen Diensten | Im Code vorbereitet bzw. nachgebildet, aber ohne eigene Azure-Subscription nicht "echt" im Einsatz - siehe Abschnitt 7 |
| **SAP** | Eine weit verbreitete Enterprise-Software, in echten Banken oft die Quelle für Kundenstammdaten | Die Anbindung ist als austauschbarer Baustein gebaut (siehe Abschnitt 7), ohne echtes SAP-System |

## 5. Welche Architektur steckt dahinter?

"Architektur" heißt hier: wie der Code in Teile aufgeteilt ist und welche Regeln dafür gelten, damit das Ganze auch bei wachsendem Umfang verständlich und änderbar bleibt.

### Fünf Schichten (Clean Architecture)

Der Code ist in fünf Projekte aufgeteilt, die man sich wie eine Zwiebel vorstellen kann - innen das Wichtigste und Stabilste, außen das, was sich am ehesten ändert:

- **Banking.Domain** (Kern): reine Geschäftsregeln - was ist ein Kunde, ein Konto, eine Zahlung, welche Zustände darf eine Zahlung durchlaufen. Kennt nichts Technisches (keine Datenbank, kein Web).
- **Banking.Application**: die "Anwendungsfälle", z. B. "Kunde suchen" oder "Zahlung anlegen".
- **Banking.Infrastructure**: die technische Umsetzung - hier lebt die Datenbank-Anbindung (EF Core), die SAP-Anbindung, der CSV-Import.
- **Banking.Api**: die REST-Schnittstelle nach außen.
- **Banking.Web**: die Weboberfläche für Mitarbeiter - spricht **ausschließlich** über die REST-API mit dem Rest des Systems, nie direkt mit der Datenbank.

Die Grundregel: äußere Schichten dürfen innere kennen, nie umgekehrt. Der fachliche Kern (Domain) weiß z. B. nichts davon, dass es überhaupt eine Weboberfläche oder eine SQL-Datenbank gibt.

### Weitere Architekturprinzipien

- **Modular Monolith statt Microservices**: Ein einzelnes, aber intern klar unterteiltes System statt vieler kleiner, unabhängig laufender Programme - für dieses Projekt einfacher zu bauen und zu verstehen, ohne die zusätzliche Komplexität von Microservices.
- **Domain-Driven Design (pragmatisch)**: Der Code spricht möglichst dieselbe Sprache wie das Fachgebiet (z. B. "Konto", "Zahlungsauftrag", nicht nur abstrakte Datensätze).
- **Anti-Corruption Layer** für externe Systeme (Azure, SAP): Die Anwendung kennt nach außen nur ein eigenes, einfaches Interface; die eigentliche Anbindung an ein fremdes System steckt dahinter versteckt und ist austauschbar.
- **"Prepared but inactive"-Muster**: Für jeden Azure-Dienst, der mangels eigener Subscription nicht real genutzt werden kann, prüft die Anwendung beim Start, ob er konfiguriert ist. Ist er es nicht, läuft alles trotzdem - mit einer lokalen, funktionierenden Alternative (z. B. Konsolen-Ausgabe statt Application Insights, ein simulierter SAP-Dienst statt echtem SAP-Zugriff).

Drei Diagramme dazu (Architektur-Schichten, Anfrage-/Anmelde-Fluss, System-Kontext) gibt es in der [Einsteiger-Dokumentation](Einsteiger-Dokumentation.docx).

## 6. Was macht die Anwendung konkret?

- **Kunden verwalten**: suchen, Details ansehen.
- **Konten einsehen**: Kontostand, Status, zugehöriger Kunde.
- **Transaktionen durchsuchen**: nach Konto, Zeitraum, Status filtern.
- **Zahlungsaufträge**: anlegen, zur Freigabe einreichen, genehmigen oder ablehnen - mit einem klaren Rollenmodell (ein einfacher Sachbearbeiter darf anlegen, aber nicht selbst genehmigen).
- **Anmeldung mit Rollen**: drei Demo-Rollen (Sachbearbeiter, Freigeber, Administrator) mit unterschiedlichen Rechten, sowohl in der Oberfläche als auch - das ist der wichtige Teil - unabhängig davon direkt in der API abgesichert.
- **Kundenprofil-Anreicherung ("Customer 360")**: zusätzlich zu den eigenen Daten werden - simuliert - Stammdaten aus einem externen System (SAP) angezeigt, mit sauberer Fehlerbehandlung, falls dieses System gerade nicht erreichbar ist.
- **Täglicher Datenimport**: eine CSV-Datei mit Transaktionen kann hochgeladen werden; die Anwendung prüft jede Zeile, wandelt sie in echte Kontobewegungen um, erkennt doppelt gelieferte Datensätze und zeigt den gesamten Ablauf inklusive Fehlern in einer eigenen Übersicht.
- **Beobachtbarkeit im Betrieb**: Jede Anfrage lässt sich technisch nachverfolgen (von der Web-Oberfläche über die API bis zur Datenbank-Abfrage), inklusive Kennzahlen wie "wie viele Zahlungen sind gerade fehlgeschlagen und warum".

## 7. Was ist "echt" und was ist simuliert?

Ehrlichkeit darüber ist ein zentrales Element dieses Projekts. Grob gilt: **alles, was ohne Bezahlung/Firmenvertrag lokal nutzbar ist, ist echt gebaut und läuft auch wirklich** - alles, was eine bezahlte Cloud-Ressource oder ein fremdes Firmensystem bräuchte, ist als **funktionsfähige, austauschbare Ersatzlösung** gebaut, mit demselben Code-Pfad, der bei einer echten Anbindung genutzt würde.

| Bereich | Status |
|---|---|
| Datenbank, REST-API, Weboberfläche, Anmeldung/Rollen | Echt, läuft lokal vollständig |
| Azure Key Vault / App Configuration | Im Code vorbereitet, mangels Azure-Subscription inaktiv |
| Application Insights | Läuft lokal über die Konsole; Azure-Anbindung vorbereitet, aber ungetestet (kein Azure-Zugang) |
| SAP-Anbindung | Läuft lokal über einen realistischen Ersatzdienst (Mock); ein echter, funktionierender Netzwerk-Client existiert ebenfalls, hat aber kein echtes SAP-System als Gegenstelle |
| Azure Data Factory (Datenimport) | Die fachliche Verarbeitung (Prüfen, Umwandeln, Speichern) ist echter Code; die Cloud-Orchestrierung drumherum ist durch einen einfachen Datei-Upload ersetzt |

## 8. Wie wird das Projekt getestet?

Hier ist der ehrliche Stand (siehe auch [`Fortschritt.md`](../Fortschritt.md)):

- **Bisher**: Jede neu gebaute Funktion wurde während der Entwicklung **manuell live getestet** - die Anwendung wurde tatsächlich gestartet und über echte HTTP-Aufrufe (curl) bzw. im Browser ausprobiert, nicht nur "sollte laut Code funktionieren". Für Kapitel wie Application Insights oder die SAP-Anbindung wurde sogar gezielt ein Fehlerfall erzwungen, um zu prüfen, ob die Anwendung sauber darauf reagiert.
- **Was noch fehlt**: Eine automatisierte Testsuite (Unit-Tests für einzelne Bausteine, Integrationstests für das Zusammenspiel) existiert bisher **nicht** - die beiden dafür vorgesehenen Testprojekte enthalten aktuell nur die Standard-Vorlage ohne echten Inhalt. Das ist im Anforderungsplan als eigenes, noch offenes Kapitel vorgesehen.

## 9. Wo finde ich mehr?

- [`README.md`](../README.md) - Kurzüberblick, Setup-Befehle
- [`Fortschritt.md`](../Fortschritt.md) - aktueller Umsetzungsstand aller 24 Kapitel
- [`docs/Einsteiger-Dokumentation.docx`](Einsteiger-Dokumentation.docx) - ausführlichere Version dieses Guides mit Diagrammen, Glossar und Code-Mustern
- [`docs/architecture/`](architecture/) - technische Detaildokumentation für erfahrene Entwickler
- [`docs/adr/decisions-backlog.md`](adr/decisions-backlog.md) - alle bewussten Abweichungen vom ursprünglichen Plan mit Begründung
- [`docs/prompt/ProjektErstellung.docx`](prompt/ProjektErstellung.docx) - das ursprüngliche, kapitelweise abgearbeitete Anforderungsdokument
