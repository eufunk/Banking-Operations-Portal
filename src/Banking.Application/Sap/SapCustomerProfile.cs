namespace Banking.Application.Sap;

/// <summary>
/// Kundenstammdaten, wie SAP sie führt - bewusst ein eigener Typ, nicht die Domain-Entity
/// <c>Customer</c>: SAP hat andere/zusätzliche Felder (Adresse, Risikoeinstufung) und eine
/// eigene Datenhoheit über sie. Das Mapping SAP-Modell -&gt; internes Modell passiert explizit
/// an der Systemgrenze (siehe SapRfcCustomerService), nie implizit.
/// </summary>
public sealed record SapCustomerProfile(
    string TaxId,
    string Street,
    string PostalCode,
    string City,
    string Country,
    SapRiskRating RiskRating,
    DateTimeOffset SyncedAt);
