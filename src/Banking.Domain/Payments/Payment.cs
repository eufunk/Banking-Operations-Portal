using Banking.Domain.Accounts;
using Banking.Domain.Common;
using Banking.Domain.Common.Exceptions;

namespace Banking.Domain.Payments;

/// <summary>
/// Zahlungsauftrag. TargetAccountNumber ist bewusst keine Referenz auf ein eigenes
/// Account-Aggregat, sondern ein reiner Wert (Payments können an externe, bankfremde
/// IBANs gehen) - daher Wiederverwendung des AccountNumber-Value-Objects, keine FK.
/// </summary>
public sealed class Payment : Entity<PaymentId>
{
    public AccountId SourceAccountId { get; private set; }
    public AccountNumber TargetAccountNumber { get; private set; }
    public Money Amount { get; private set; }
    public PaymentReference Reference { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Für EF Core.
    private Payment()
    {
    }

    private Payment(
        PaymentId id,
        AccountId sourceAccountId,
        AccountNumber targetAccountNumber,
        Money amount,
        PaymentReference reference,
        PaymentStatus status,
        DateTime createdAt)
        : base(id)
    {
        SourceAccountId = sourceAccountId;
        TargetAccountNumber = targetAccountNumber;
        Amount = amount;
        Reference = reference;
        Status = status;
        CreatedAt = createdAt;
    }

    public static Payment Create(AccountId sourceAccountId, AccountNumber targetAccountNumber, Money amount, PaymentReference reference)
    {
        if (amount.Amount <= 0)
            throw new ArgumentException("Der Zahlungsbetrag muss größer als 0 sein.", nameof(amount));

        return new Payment(
            PaymentId.New(),
            sourceAccountId,
            targetAccountNumber,
            amount,
            reference,
            PaymentStatus.Draft,
            DateTime.UtcNow);
    }

    public void SubmitForApproval() => TransitionTo(PaymentStatus.PendingApproval, PaymentStatus.Draft);

    public void Approve() => TransitionTo(PaymentStatus.Approved, PaymentStatus.PendingApproval);

    public void Reject() => TransitionTo(PaymentStatus.Rejected, PaymentStatus.PendingApproval, PaymentStatus.Approved);

    public void MarkSubmittedToNetwork() => TransitionTo(PaymentStatus.Submitted, PaymentStatus.Approved);

    public void MarkExecuted() => TransitionTo(PaymentStatus.Executed, PaymentStatus.Submitted);

    public void MarkFailed() => TransitionTo(PaymentStatus.Failed, PaymentStatus.Submitted);

    private void TransitionTo(PaymentStatus target, params PaymentStatus[] allowedFrom)
    {
        if (!allowedFrom.Contains(Status))
            throw new InvalidStatusTransitionException(nameof(Payment), Status.ToString(), target.ToString());

        Status = target;
    }
}
