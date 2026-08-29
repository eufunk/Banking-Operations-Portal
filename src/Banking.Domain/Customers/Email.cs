using System.Text.RegularExpressions;

namespace Banking.Domain.Customers;

public readonly record struct Email
{
    public const int MaxLength = 256;

    private static readonly Regex Pattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("E-Mail darf nicht leer sein.", nameof(value));
        if (value.Length > MaxLength)
            throw new ArgumentException($"E-Mail darf maximal {MaxLength} Zeichen lang sein.", nameof(value));
        if (!Pattern.IsMatch(value))
            throw new ArgumentException($"'{value}' ist keine gültige E-Mail-Adresse.", nameof(value));

        Value = value.Trim();
    }

    public override string ToString() => Value;
}
