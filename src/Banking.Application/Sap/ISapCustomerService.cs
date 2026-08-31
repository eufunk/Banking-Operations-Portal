using Banking.Domain.Accounts;
using Banking.Domain.Customers;

namespace Banking.Application.Sap;

/// <summary>
/// Port zu SAP als externem Master-Data-System (Anti-Corruption Layer, siehe ADR #9/#19).
/// Domain und Application kennen ausschließlich dieses Interface, nie SAP-spezifische Typen
/// oder Bibliotheken (SAP .NET Connector/RFC) - die tatsächliche Anbindung
/// (<c>SapRfcCustomerService</c>, <c>MockSapCustomerService</c>) lebt ausschließlich in
/// Banking.Infrastructure.
///
/// Rückgabewert <c>null</c> bedeutet: SAP kennt diesen Kunden/dieses Konto nicht (fachlicher
/// Fall, kein Fehler). Bei technischen Problemen (Timeout, Verbindungsfehler, unerwartete
/// Antwort) wird stattdessen eine <see cref="SapIntegrationException"/> geworfen - der
/// Aufrufer entscheidet, wie er mit dem Ausfall einer externen Abhängigkeit umgeht (siehe
/// GetCustomerSapProfileHandler: graceful degradation statt Komplettausfall der Anfrage).
/// </summary>
public interface ISapCustomerService
{
    public Task<SapCustomerProfile?> GetCustomerAsync(CustomerNumber customerNumber, CancellationToken cancellationToken);

    public Task<SapAccountInfo?> GetAccountAsync(AccountNumber accountNumber, CancellationToken cancellationToken);
}
