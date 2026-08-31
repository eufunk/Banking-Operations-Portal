namespace Banking.Application.Common.Options;

/// <summary>
/// "Configuration" - kein Secret. Leere/fehlende <see cref="BaseUrl"/> = kein SAP-System
/// verfügbar (Standardfall in diesem Projekt) - Banking.Infrastructure registriert dann
/// <c>MockSapCustomerService</c> statt der echten, HttpClient-basierten Anbindung (siehe
/// InfrastructureServiceCollectionExtensions und ADR #19).
/// </summary>
public sealed class SapOptions
{
    public const string SectionName = "Sap";

    public Uri? BaseUrl { get; set; }

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public int MaxRetryAttempts { get; set; } = 3;
}
