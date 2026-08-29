using Banking.Domain.Accounts;
using Banking.Domain.Common;
using Banking.Domain.Common.Exceptions;

namespace Banking.Domain.Transactions;

/// <summary>
/// Eigenes Aggregat statt Kindliste von <see cref="Account"/> - ein Konto kann über
/// Jahre zehntausende Buchungen ansammeln, das würde das Account-Aggregat sprengen.
/// Konsequenz: Account.Balance ist ein separat gepflegter, denormalisierter Wert
/// (siehe docs/architecture/modules-and-domain.md).
/// </summary>
public sealed class Transaction : Entity<TransactionId>
{
    public const int MaxDescriptionLength = 500;

    public AccountId AccountId { get; private set; }
    public Money Amount { get; private set; }
    public DateTime BookingDate { get; private set; }
    public string? Description { get; private set; }
    public TransactionType TransactionType { get; private set; }
    public TransactionStatus Status { get; private set; }

    // Für EF Core.
    private Transaction()
    {
    }

    private Transaction(
        TransactionId id,
        AccountId accountId,
        Money amount,
        DateTime bookingDate,
        string? description,
        TransactionType transactionType,
        TransactionStatus status)
        : base(id)
    {
        AccountId = accountId;
        Amount = amount;
        BookingDate = bookingDate;
        Description = description;
        TransactionType = transactionType;
        Status = status;
    }

    public static Transaction Create(
        AccountId accountId,
        Money amount,
        TransactionType transactionType,
        string? description = null,
        DateTime? bookingDate = null)
    {
        if (amount.Amount <= 0)
            throw new ArgumentException("Der Transaktionsbetrag muss größer als 0 sein.", nameof(amount));
        if (description is { Length: > MaxDescriptionLength })
            throw new ArgumentException($"Beschreibung darf maximal {MaxDescriptionLength} Zeichen lang sein.", nameof(description));

        return new Transaction(
            TransactionId.New(),
            accountId,
            amount,
            bookingDate ?? DateTime.UtcNow,
            description?.Trim(),
            transactionType,
            TransactionStatus.Pending);
    }

    public bool IncreasesBalance => TransactionType is TransactionType.Deposit
        or TransactionType.TransferIn
        or TransactionType.Interest
        or TransactionType.Reversal;

    public void MarkBooked() => TransitionTo(TransactionStatus.Booked, TransactionStatus.Pending);

    public void MarkFailed() => TransitionTo(TransactionStatus.Failed, TransactionStatus.Pending);

    public void Reverse() => TransitionTo(TransactionStatus.Reversed, TransactionStatus.Booked);

    private void TransitionTo(TransactionStatus target, params TransactionStatus[] allowedFrom)
    {
        if (!allowedFrom.Contains(Status))
            throw new InvalidStatusTransitionException(nameof(Transaction), Status.ToString(), target.ToString());

        Status = target;
    }
}
