# Azure Services & ihr Zweck im Projekt

| Service | Zweck im Banking Operations Portal | Relevante Phase |
|---|---|---|
| **Azure App Service** | Hosting von `Banking.Api` und `Banking.Web` (zwei App-Service-Instanzen oder Slots) | 15 |
| **Azure SQL Database** | Primäre relationale Datenbank für alle Module (Customers, Accounts, Transactions, Payments, …) | 3, 15 |
| **Azure SQL Elastic Pool** | Kosteneffiziente Skalierung, falls mehrere Datenbanken (z. B. pro Umgebung oder simuliertem Mandanten/Filiale) gemeinsame Ressourcen nutzen sollen | 15 |
| **Azure Key Vault** | Speicherung von Secrets: Connection Strings, API-Keys für externe Systeme, Zertifikate – niemals in `appsettings.json` oder Git | 7, 15 |
| **Azure App Configuration** | Zentrale, nicht-geheime Konfiguration (Feature Flags, Umgebungsparameter), verweist bei Bedarf per Key-Vault-Reference auf Secrets | 15 |
| **Application Insights** | Distributed Tracing, strukturiertes Logging, Metriken, Fehler-Alerts – Basis für das Monitoring-Modul | 8, 14 |
| **Azure Data Factory** | Orchestrierung von Batch-Importen (z. B. nächtlicher Transaktions-Import), meldet Job-Status an das `DataImports`-Modul | 12 |
| **Managed Identity** | Passwortlose Authentifizierung von App Service zu Key Vault, Azure SQL und ggf. Data Factory – vermeidet Secrets für Azure-interne Zugriffe komplett | 7, 15 |
| **Azure DevOps** | Repository-Hosting (optional, falls nicht GitHub), Work-Item-Tracking, CI/CD-Pipelines | 16 |
| **JFrog Artifactory** | Verwaltung interner NuGet-Pakete, falls wir Module später als wiederverwendbare Packages auslagern; simuliert reale Enterprise-Artefaktverwaltung | 16 |

**Designprinzip:** Jeder Azure-Service wird erst eingeführt, wenn er im Implementierungsplan tatsächlich gebraucht wird (siehe "Relevante Phase") – kein Over-Provisioning am Anfang. Lokale Entwicklung läuft mit lokalen Äquivalenten (LocalDB/SQLite, User Secrets, Konsolen-Logging), bevor wir auf echte Azure-Ressourcen umstellen.
