namespace Banking.Web.Models;

// Eigene, schlanke Enums für die UI - bewusst keine Referenz auf Banking.Domain
// (ADR-0002: Banking.Web spricht ausschließlich über die Api-Verträge, nie die
// internen Modelle der anderen Schichten direkt an).

public enum CustomerStatus
{
    Active,
    Inactive,
    Blocked,
    Closed,
}

public enum AccountType
{
    Checking,
    Savings,
    Credit,
}

public enum AccountStatus
{
    Active,
    Blocked,
    Closed,
}

public enum TransactionType
{
    Deposit,
    Withdrawal,
    TransferIn,
    TransferOut,
    Fee,
    Interest,
    Reversal,
}

public enum TransactionStatus
{
    Pending,
    Booked,
    Reversed,
    Failed,
}

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
