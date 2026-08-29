namespace Banking.Domain.Transactions;

public enum TransactionStatus
{
    Pending,
    Booked,
    Reversed,
    Failed,
}
