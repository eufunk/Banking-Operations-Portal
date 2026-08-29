using Banking.Domain.Common.Exceptions;

namespace Banking.Domain.Common;

/// <summary>
/// Geldbetrag + Währung als untrennbare Einheit. Verhindert, dass ein nackter
/// decimal ohne Währungsbezug durch die Domain gereicht wird, und dass Rechen-
/// operationen zwischen unterschiedlichen Währungen versehentlich zugelassen werden.
/// </summary>
public readonly record struct Money
{
    public decimal Amount { get; }
    public CurrencyCode Currency { get; }

    public Money(decimal amount, CurrencyCode currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Zero(CurrencyCode currency) => new(0m, currency);

    /// <summary>Factory für Beträge, die fachlich zwingend positiv sein müssen (Zahlungen, Buchungsbeträge).</summary>
    public static Money Positive(decimal amount, CurrencyCode currency)
    {
        if (amount <= 0)
            throw new ArgumentException("Der Betrag muss größer als 0 sein.", nameof(amount));

        return new Money(amount, currency);
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public bool IsNegative => Amount < 0;

    private void EnsureSameCurrency(Money other)
    {
        if (!Currency.Equals(other.Currency))
            throw new CurrencyMismatchException(Currency, other.Currency);
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
