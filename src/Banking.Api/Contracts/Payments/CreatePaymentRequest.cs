namespace Banking.Api.Contracts.Payments;

/// <summary>
/// Eigener Api-Vertrag statt direktem Binding von CreatePaymentCommand: der Wire-Contract
/// (JSON-Form, welche Felder Pflicht sind, wie sie heißen) und das interne Command haben
/// unterschiedliche Änderungsgründe. Eine Api-Versionierung soll nicht automatisch die
/// interne Command-Struktur beeinflussen, und umgekehrt.
/// </summary>
public sealed record CreatePaymentRequest(
    Guid SourceAccountId,
    string TargetAccountNumber,
    decimal Amount,
    string Currency,
    string Reference);
