using Banking.Application.Sap;
using Banking.Domain.Accounts;
using Banking.Domain.Customers;
using Microsoft.Extensions.Logging;

namespace Banking.Infrastructure.Sap;

/// <summary>
/// Simuliert SAP für lokale Entwicklung/Demo ohne echtes SAP-System (siehe ADR #19). Liefert
/// deterministische Fake-Daten, abgeleitet aus einem stabilen Hash der jeweiligen Nummer -
/// derselbe Kunde/dasselbe Konto liefert bei jedem Aufruf (auch über Prozess-Neustarts hinweg)
/// dieselben Werte, so wie ein echtes System auch. Simuliert zusätzlich realistische Latenz
/// und - für etwa jeden siebten Schlüssel, reproduzierbar - einen SAP-Ausfall, damit der
/// Graceful-Degradation-Pfad in GetCustomerSapProfileHandler auch lokal ohne echte SAP-Störung
/// nachvollziehbar bleibt.
/// </summary>
internal sealed class MockSapCustomerService : ISapCustomerService
{
    private readonly ILogger<MockSapCustomerService> _logger;

    public MockSapCustomerService(ILogger<MockSapCustomerService> logger)
    {
        _logger = logger;
    }

    public async Task<SapCustomerProfile?> GetCustomerAsync(CustomerNumber customerNumber, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mock-SAP: Kundenabfrage für {CustomerNumber}", customerNumber);

        await Task.Delay(SimulatedLatency(), cancellationToken);
        SimulateOutageIfApplicable(customerNumber.Value);

        var seed = StableHash(customerNumber.Value);
        return new SapCustomerProfile(
            TaxId: $"DE{seed % 1_000_000_000:D9}",
            Street: $"Musterstraße {seed % 200 + 1}",
            PostalCode: (10000 + seed % 89999).ToString(),
            City: "Berlin",
            Country: "DE",
            RiskRating: (SapRiskRating)(seed % 3),
            SyncedAt: DateTimeOffset.UtcNow);
    }

    public async Task<SapAccountInfo?> GetAccountAsync(AccountNumber accountNumber, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mock-SAP: Kontoabfrage für {AccountNumber}", accountNumber);

        await Task.Delay(SimulatedLatency(), cancellationToken);
        SimulateOutageIfApplicable(accountNumber.Value);

        var seed = StableHash(accountNumber.Value);
        return new SapAccountInfo(
            SapAccountStatus: seed % 2 == 0 ? "FREIGEGEBEN" : "IN_PRUEFUNG",
            IbanVerified: seed % 3 != 0,
            SyncedAt: DateTimeOffset.UtcNow);
    }

    private static TimeSpan SimulatedLatency() => TimeSpan.FromMilliseconds(150 + Random.Shared.Next(250));

    private static void SimulateOutageIfApplicable(string key)
    {
        if (StableHash(key) % 7 == 0)
        {
            throw new SapIntegrationException($"Simulierter SAP-Timeout (Mock) für Schlüssel '{key}'.");
        }
    }

    // string.GetHashCode() ist pro Prozesslauf randomisiert (Sicherheitsfeature von .NET) -
    // für reproduzierbare Demo-/Testergebnisse über Neustarts hinweg brauchen wir stattdessen
    // einen stabilen, einfachen Hash. Maskierung statt Math.Abs, da Math.Abs(int.MinValue)
    // eine OverflowException werfen würde.
    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in value)
            {
                hash = (hash * 31) + c;
            }

            return hash & int.MaxValue;
        }
    }
}
