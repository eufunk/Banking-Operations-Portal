namespace Banking.Domain.Payments;

/// <summary>Verwendungszweck. Längenlimit orientiert sich am SEPA-Remittance-Info-Standard (140 Zeichen).</summary>
public readonly record struct PaymentReference
{
    public const int MaxLength = 140;

    public string Value { get; }

    public PaymentReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Verwendungszweck darf nicht leer sein.", nameof(value));
        if (value.Length > MaxLength)
            throw new ArgumentException($"Verwendungszweck darf maximal {MaxLength} Zeichen lang sein.", nameof(value));

        Value = value.Trim();
    }

    public override string ToString() => Value;
}
