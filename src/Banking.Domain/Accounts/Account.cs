using Banking.Domain.Common;
using Banking.Domain.Common.Exceptions;
using Banking.Domain.Customers;

namespace Banking.Domain.Accounts;

public sealed class Account : Entity<AccountId>
{
    public AccountNumber AccountNumber { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public AccountType AccountType { get; private set; }
    public CurrencyCode Currency { get; private set; }
    public AccountStatus Status { get; private set; }

    /// <summary>
    /// Nur intern gepflegter Saldo. Absichtlich kein öffentlicher Setter - jede
    /// Änderung muss über <see cref="Debit"/>/<see cref="Credit"/> laufen, damit
    /// Währung, Kontostatus und Deckung geprüft werden (siehe docs/architecture).
    /// </summary>
    private decimal BalanceAmount { get; set; }

    public Money Balance => new(BalanceAmount, Currency);

    // Für EF Core.
    private Account()
    {
    }

    private Account(
        AccountId id,
        AccountNumber accountNumber,
        CustomerId customerId,
        AccountType accountType,
        CurrencyCode currency,
        AccountStatus status)
        : base(id)
    {
        AccountNumber = accountNumber;
        CustomerId = customerId;
        AccountType = accountType;
        Currency = currency;
        Status = status;
        BalanceAmount = 0m;
    }

    public static Account Open(AccountNumber accountNumber, CustomerId customerId, AccountType accountType, CurrencyCode currency)
        => new(AccountId.New(), accountNumber, customerId, accountType, currency, AccountStatus.Active);

    public void Credit(Money amount)
    {
        EnsureActive();
        EnsurePositive(amount);
        EnsureCurrencyMatches(amount);

        BalanceAmount += amount.Amount;
    }

    public void Debit(Money amount)
    {
        EnsureActive();
        EnsurePositive(amount);
        EnsureCurrencyMatches(amount);

        var resultingBalance = BalanceAmount - amount.Amount;

        // Girokonto/Sparkonto dürfen nicht ins Minus rutschen, ein Kreditkonto (mit vereinbartem Rahmen) schon.
        if (resultingBalance < 0 && AccountType != AccountType.Credit)
            throw new InsufficientFundsException(AccountNumber.ToString(), amount, Balance);

        BalanceAmount = resultingBalance;
    }

    public void Block() => TransitionTo(AccountStatus.Blocked, AccountStatus.Active);

    public void Reactivate() => TransitionTo(AccountStatus.Active, AccountStatus.Blocked);

    public void Close() => TransitionTo(AccountStatus.Closed, AccountStatus.Active, AccountStatus.Blocked);

    private void TransitionTo(AccountStatus target, params AccountStatus[] allowedFrom)
    {
        if (Status == AccountStatus.Closed || !allowedFrom.Contains(Status))
            throw new InvalidStatusTransitionException(nameof(Account), Status.ToString(), target.ToString());

        Status = target;
    }

    private void EnsureActive()
    {
        if (Status != AccountStatus.Active)
            throw new InvalidOperationException($"Konto {AccountNumber} ist nicht aktiv (Status: {Status}) und kann nicht bebucht werden.");
    }

    private static void EnsurePositive(Money amount)
    {
        if (amount.Amount <= 0)
            throw new ArgumentException("Der Buchungsbetrag muss größer als 0 sein.", nameof(amount));
    }

    private void EnsureCurrencyMatches(Money amount)
    {
        if (!amount.Currency.Equals(Currency))
            throw new CurrencyMismatchException(Currency, amount.Currency);
    }
}
