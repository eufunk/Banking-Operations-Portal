namespace Banking.Domain.Common.Exceptions;

public sealed class InsufficientFundsException : DomainException
{
    public InsufficientFundsException(string accountNumber, Money attemptedDebit, Money currentBalance)
        : base($"Konto {accountNumber}: Belastung über {attemptedDebit} bei Saldo {currentBalance} ist nicht gedeckt.")
    {
    }
}
