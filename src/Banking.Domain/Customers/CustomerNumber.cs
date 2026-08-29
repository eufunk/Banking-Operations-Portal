namespace Banking.Domain.Customers;

public readonly record struct CustomerNumber
{
    public const int MaxLength = 20;

    public string Value { get; }

    public CustomerNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Kundennummer darf nicht leer sein.", nameof(value));
        if (value.Length > MaxLength)
            throw new ArgumentException($"Kundennummer darf maximal {MaxLength} Zeichen lang sein.", nameof(value));

        Value = value.Trim();
    }

    public override string ToString() => Value;
}
