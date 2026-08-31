# Configuration Management

## Secrets vs. Configuration - die Grundregel

Wir trennen konsequent zwei Kategorien, die im Alltag gern vermischt werden:

| | **Secrets** | **Configuration** |
|---|---|---|
| Beispiele in diesem Projekt | Database-Connection-String, JWT-Signing-Key, (später) SAP-Zugangsdaten, API-Keys | Payment Limits, Timeouts, Feature Flags, Application Settings |
| Darf im Repo stehen? | **Nie** | Ja, unbedenklich |
| Wo gepflegt (lokal) | User Secrets (`dotnet user-secrets`) | `appsettings.{Environment}.json` |
| Wo gepflegt (Azure) | Azure Key Vault | Azure App Configuration |
| Zugriff in Azure | Managed Identity (kein Passwort im Code) | Managed Identity oder Connection String |

Faustregel: **Wenn der Wert bei einem Leak einem Angreifer nützt (Zugriff auf ein System), ist es ein Secret. Wenn er nur das Verhalten der Anwendung steuert, ist es Configuration.**

## Die sechs Mechanismen im Vergleich

| Mechanismus | Zweck | Persistenz | Sichtbarkeit |
|---|---|---|---|
| **appsettings.json** | Default-Werte, eingecheckt | Datei im Repo | Für jeden mit Repo-Zugriff sichtbar - **nie Secrets hier** |
| **Environment Variables** | Overrides zur Laufzeit (Container, CI/CD, Azure App Service "Application Settings") | Prozessumgebung, nicht im Repo | Sichtbar für alle mit Zugriff auf den Host/die Pipeline-Konfiguration |
| **User Secrets** | Lokale Entwickler-Secrets | `%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json`, außerhalb des Repo-Ordners | Nur auf der eigenen Maschine, pro Entwickler unterschiedlich |
| **Azure App Configuration** | Zentral verwaltete, nicht-geheime Einstellungen + Feature Flags, für mehrere Dienste/Instanzen gemeinsam | Azure-Ressource | Team-weit, mit Zugriffssteuerung über Azure RBAC |
| **Azure Key Vault** | Zentral verwaltete Secrets | Azure-Ressource, verschlüsselt | Nur mit expliziter Berechtigung (RBAC/Access Policies) abrufbar |
| **Managed Identity** | Kein Konfigurationswert, sondern der Authentifizierungsmechanismus, mit dem eine Azure-Ressource (z. B. unser App Service) sich bei Key Vault/App Configuration ausweist - **ohne dass irgendwo ein Passwort/Client Secret hinterlegt werden muss** | Von Azure verwaltet, nicht konfigurierbar | Identität ist an die Azure-Ressource gebunden, nicht an einen Nutzer |

**Warum Managed Identity kein siebter "Konfigurationsort" ist:** Es ist die Antwort auf die Frage "Wie beweist unsere Anwendung gegenüber Key Vault/App Configuration, wer sie ist?" - ohne Managed Identity bräuchte man dafür selbst wieder ein Secret (einen Client-Secret/Zertifikat für einen Service Principal), das dann irgendwo sicher hinterlegt werden müsste. Managed Identity löst genau dieses Henne-Ei-Problem: die Azure-Plattform selbst verbürgt sich für die Identität der Ressource.

## Finale Configuration-Hierarchie (spätere Priorität überschreibt frühere)

```
1. appsettings.json                    Defaults, eingecheckt
2. appsettings.{Environment}.json      Development/Production-Overrides, eingecheckt
3. User Secrets                        NUR in Development, nie eingecheckt
4. Environment Variables               Container-/Pipeline-Overrides
5. Azure App Configuration             zentrale Configuration + Feature Flags (nur wenn konfiguriert)
     └─ Key Vault References darin    verweisen transparent auf echte Secrets in Key Vault
6. Azure Key Vault (direkt)            zusätzliche Secrets ohne Umweg über App Configuration
```

Das spiegelt exakt die Reihenfolge der `.Add...()`-Aufrufe in `Program.cs` von `Banking.Api` und `Banking.Web` wider (siehe dort) - wer zuletzt für einen Schlüssel registriert wird, gewinnt.

## Aktueller Stand in diesem Projekt

Da keine Azure-Subscription zur Verfügung stand (siehe [decisions-backlog.md](../adr/decisions-backlog.md) #17), sind die Schritte 5-6 **vorbereitet, aber inaktiv**: `Program.cs` prüft, ob `AzureAppConfiguration:Endpoint` bzw. `KeyVault:Uri` konfiguriert sind, und überspringt die entsprechenden `Add...()`-Aufrufe sonst vollständig. Lokal laufen wir ausschließlich mit den Schritten 1-4:

- **Secrets** (`dotnet user-secrets`): `ConnectionStrings:BankingDatabase`, `Jwt:SigningKey`, `Jwt:Issuer`, `Jwt:Audience`
- **Configuration** (`appsettings.json`/`appsettings.Development.json`): `PaymentLimits:MaxAmountPerPayment`, `Timeouts:DatabaseCommandTimeout`, `FeatureFlags:EnablePaymentApprovalWorkflow`

Die Configuration-Werte sind an echtes Verhalten gebunden, nicht nur deklariert:
- `PaymentLimits.MaxAmountPerPayment` → `CreatePaymentValidator` lehnt größere Beträge ab
- `Timeouts.DatabaseCommandTimeout` → EF-Core-`CommandTimeout` in `BankingDbContext`
- `FeatureFlags.EnablePaymentApprovalWorkflow` → steuert, ob `CreatePaymentHandler` neue Zahlungen automatisch zur Freigabe einreicht

`FeatureFlagsOptions` wird über `IOptionsMonitor<T>` statt `IOptions<T>` injiziert - das ist die Stelle im Code, die später von Azure App Configurations Live-Reload (ohne Neustart der Anwendung) profitieren würde.

## Development/Test/Production

ASP.NET Core wählt die Umgebung über `ASPNETCORE_ENVIRONMENT` und lädt entsprechend `appsettings.{Environment}.json` obendrauf. Aktuell vorhanden:

- `appsettings.Development.json` - höheres `PaymentLimits.MaxAmountPerPayment` fürs bequeme manuelle Testen
- `appsettings.Production.json` - strengeres Logging (`Warning` statt `Information`), Platzhalter für `AzureAppConfiguration:Endpoint`/`KeyVault:Uri`, die im Zielbild als Azure App Service "Application Settings" (Umgebungsvariablen) gesetzt würden, nicht in dieser Datei

Eine eigene "Test"-Umgebung existiert noch nicht (kein bisher gebrauchter Anwendungsfall), ließe sich aber identisch als `appsettings.Test.json` + `ASPNETCORE_ENVIRONMENT=Test` ergänzen.
