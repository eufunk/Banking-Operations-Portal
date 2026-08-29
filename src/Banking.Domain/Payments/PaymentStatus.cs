namespace Banking.Domain.Payments;

public enum PaymentStatus
{
    Draft,
    PendingApproval,
    Approved,
    Submitted,
    Executed,
    Rejected,
    Failed,
}
