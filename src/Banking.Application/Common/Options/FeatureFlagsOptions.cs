namespace Banking.Application.Common.Options;

/// <summary>
/// "Configuration" - Business-Schalter, kein Secret. In Azure würde dieser Wert typisch
/// über Azure App Configuration Feature Flags gepflegt (mit Live-Reload, ohne Deployment).
/// </summary>
public sealed class FeatureFlagsOptions
{
    public const string SectionName = "FeatureFlags";

    /// <summary>
    /// Wenn deaktiviert, werden neu erstellte Zahlungen nicht automatisch zur Freigabe
    /// eingereicht (bleiben im Status Draft) - z. B. nützlich, um den Freigabe-Workflow
    /// für eine Testumgebung temporär abzuschalten, ohne Code zu ändern.
    /// </summary>
    public bool EnablePaymentApprovalWorkflow { get; set; } = true;
}
