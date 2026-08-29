namespace Banking.Web.Models;

public sealed record DashboardSummary(
    int CustomerCount,
    int AccountCount,
    int PaymentCount,
    int FailedPaymentCount,
    IReadOnlyList<TransactionSummary> RecentTransactions);
