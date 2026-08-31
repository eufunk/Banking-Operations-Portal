using System.Net;
using System.Net.Http.Json;
using Banking.Application.Sap;
using Banking.Domain.Accounts;
using Banking.Domain.Customers;
using Microsoft.Extensions.Logging;

namespace Banking.Infrastructure.Sap;

/// <summary>
/// Reale SAP-Anbindung. Klassisches SAP-RFC/BAPI läuft über den SAP .NET Connector (NCo) -
/// eine proprietäre, nur mit aktivem SAP-Vertrag/-Account beziehbare Bibliothek, die sich
/// nicht über NuGet installieren lässt und deshalb hier nicht referenziert werden kann (siehe
/// ADR #19). Viele reale .NET-zu-SAP-Integrationen laufen ohnehin nicht über direktes RFC vom
/// Applikationsserver aus, sondern über ein RFC-fähiges SAP-Gateway/OData-Service, das SAP
/// dafür exponiert - genau dieses (realistischere) Muster bildet diese Klasse über HttpClient
/// ab. Timeout/Retry/Circuit-Breaker kommen als Cross-Cutting-Concern über den
/// Resilience-Handler aus InfrastructureServiceCollectionExtensions
/// (Microsoft.Extensions.Http.Resilience), nicht aus dieser Klasse selbst.
/// </summary>
internal sealed class SapRfcCustomerService : ISapCustomerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SapRfcCustomerService> _logger;

    public SapRfcCustomerService(HttpClient httpClient, ILogger<SapRfcCustomerService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SapCustomerProfile?> GetCustomerAsync(CustomerNumber customerNumber, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                $"customers/{Uri.EscapeDataString(customerNumber.Value)}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var sapModel = await response.Content.ReadFromJsonAsync<SapCustomerResponse>(cancellationToken)
                ?? throw new SapIntegrationException($"SAP-Antwort für Kunde {customerNumber} konnte nicht gelesen werden.");

            // Mapping SAP-Modell -> internes Modell: SAP liefert eigene Feldnamen/Formate
            // (z. B. Risikoklasse als Zahlencode) - diese Übersetzung passiert bewusst hier,
            // direkt an der Systemgrenze, damit der Rest der Anwendung nie SAP-spezifische
            // Formate zu Gesicht bekommt.
            return new SapCustomerProfile(
                sapModel.StCd1,
                sapModel.Stras,
                sapModel.PostCode1,
                sapModel.City1,
                sapModel.Land1,
                MapRiskClass(sapModel.RiskClass),
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Der Aufrufer selbst hat abgebrochen (z. B. Browser-Request beendet) - das ist
            // kein SAP-Ausfall, sondern eine normale Cancellation, die unverändert
            // durchgereicht werden muss.
            throw;
        }
        catch (Exception ex)
        {
            // Bewusst breit gefangen: das ist die Systemgrenze zu SAP (Anti-Corruption
            // Layer). Alles, was hier technisch schiefgehen kann - Verbindungsfehler,
            // Timeout/Retry-Erschöpfung des Resilience-Handlers (Polly wirft dafür eigene
            // Typen wie TimeoutRejectedException/BrokenCircuitException, keine
            // HttpRequestException), fehlerhaftes JSON - muss als SapIntegrationException
            // herauskommen, nie als SAP-/Polly-spezifischer Exception-Typ.
            _logger.LogWarning(ex, "SAP-Kundenabfrage fehlgeschlagen für {CustomerNumber}", customerNumber);
            throw new SapIntegrationException($"SAP-Kundenabfrage für {customerNumber} fehlgeschlagen.", ex);
        }
    }

    public async Task<SapAccountInfo?> GetAccountAsync(AccountNumber accountNumber, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                $"accounts/{Uri.EscapeDataString(accountNumber.Value)}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var sapModel = await response.Content.ReadFromJsonAsync<SapAccountResponse>(cancellationToken)
                ?? throw new SapIntegrationException($"SAP-Antwort für Konto {accountNumber} konnte nicht gelesen werden.");

            return new SapAccountInfo(sapModel.AccountStatus, sapModel.IbanValidated, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SAP-Kontoabfrage fehlgeschlagen für {AccountNumber}", accountNumber);
            throw new SapIntegrationException($"SAP-Kontoabfrage für {accountNumber} fehlgeschlagen.", ex);
        }
    }

    private static SapRiskRating MapRiskClass(int sapRiskClass) => sapRiskClass switch
    {
        <= 1 => SapRiskRating.Low,
        2 => SapRiskRating.Medium,
        _ => SapRiskRating.High,
    };
}
