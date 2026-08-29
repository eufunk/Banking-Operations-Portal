namespace Banking.Domain.Accounts;

/// <summary>
/// Kontonummer/IBAN-artiger Identifier. Länge orientiert sich an der maximalen
/// IBAN-Länge (34 Zeichen) - keine vollständige IBAN-Prüfsummenvalidierung, das
/// wäre für dieses Portfolio-Projekt Overengineering.
/// </summary>
public readonly record struct AccountNumber
{
    public const int MaxLength = 34;

    public string Value { get; }

    public AccountNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Kontonummer darf nicht leer sein.", nameof(value));
        if (value.Length > MaxLength)
            throw new ArgumentException($"Kontonummer darf maximal {MaxLength} Zeichen lang sein.", nameof(value));

        Value = value.Trim().ToUpperInvariant();
    }

    public override string ToString() => Value;
}
