namespace Banking.Domain.Common.Exceptions;

public sealed class CurrencyMismatchException : DomainException
{
    public CurrencyMismatchException(CurrencyCode expected, CurrencyCode actual)
        : base($"Erwartete Währung '{expected}', erhalten '{actual}'.")
    {
    }
}
