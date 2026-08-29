namespace Banking.Domain.Common;

/// <summary>
/// Validierter ISO-4217-Währungscode. Value Object statt freiem string/enum:
/// verhindert Tippfehler (z. B. "EURO") und erlaubt neue Währungen ohne Recompile
/// der Domain, solange sie in der unterstützten Liste ergänzt werden.
/// </summary>
public readonly record struct CurrencyCode
{
    private static readonly HashSet<string> SupportedCodes = new(StringComparer.Ordinal)
    {
        "EUR", "USD", "CHF", "GBP",
    };

    public string Value { get; }

    public CurrencyCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Währungscode darf nicht leer sein.", nameof(value));

        var normalized = value.Trim().ToUpperInvariant();

        if (!SupportedCodes.Contains(normalized))
            throw new ArgumentException($"Währungscode '{value}' wird nicht unterstützt.", nameof(value));

        Value = normalized;
    }

    public static CurrencyCode Eur => new("EUR");

    public override string ToString() => Value;
}
