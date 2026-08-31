namespace Banking.Application.Common.Options;

/// <summary>
/// "Configuration", kein Secret - ein Grenzwert, keine Zugangsdaten. Gehört fachlich zur
/// Bank-Policy, nicht zum Code: eine Änderung soll ohne Neu-Deployment möglich sein
/// (lokal via appsettings, in Azure über App Configuration mit Live-Reload).
/// </summary>
public sealed class PaymentLimitsOptions
{
    public const string SectionName = "PaymentLimits";

    public decimal MaxAmountPerPayment { get; set; } = 50_000m;
}
