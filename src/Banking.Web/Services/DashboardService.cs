using Banking.Web.Models;

namespace Banking.Web.Services;

/// <summary>
/// Es gibt bewusst keinen eigenen /api/dashboard-Endpunkt - die Kennzahlen sind reine
/// Aggregationen bestehender Such-Endpunkte (TotalCount aus einer pageSize=1-Abfrage ist
/// eine gängige, günstige Methode für eine Zählung, ohne einen eigenen Count-Endpunkt zu
/// brauchen). Diese Klasse bündelt die vier parallelen Aufrufe, damit die Dashboard-Seite
/// selbst nicht wissen muss, wie die Kennzahl "Anzahl Kunden" zustande kommt.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private readonly ICustomersApiClient _customers;
    private readonly IAccountsApiClient _accounts;
    private readonly IPaymentsApiClient _payments;
    private readonly ITransactionsApiClient _transactions;

    public DashboardService(
        ICustomersApiClient customers,
        IAccountsApiClient accounts,
        IPaymentsApiClient payments,
        ITransactionsApiClient transactions)
    {
        _customers = customers;
        _accounts = accounts;
        _payments = payments;
        _transactions = transactions;
    }

    public async Task<ApiResult<DashboardSummary>> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var customersTask = _customers.SearchAsync(null, null, page: 1, pageSize: 1, cancellationToken);
        var accountsTask = _accounts.SearchAsync(null, null, page: 1, pageSize: 1, cancellationToken);
        var paymentsTask = _payments.SearchAsync(null, null, page: 1, pageSize: 1, cancellationToken);
        var failedPaymentsTask = _payments.SearchAsync(null, PaymentStatus.Failed, page: 1, pageSize: 1, cancellationToken);
        var recentTransactionsTask = _transactions.SearchAsync(null, null, null, null, page: 1, pageSize: 5, cancellationToken);

        await Task.WhenAll(customersTask, accountsTask, paymentsTask, failedPaymentsTask, recentTransactionsTask);

        var customers = await customersTask;
        var accounts = await accountsTask;
        var payments = await paymentsTask;
        var failedPayments = await failedPaymentsTask;
        var recentTransactions = await recentTransactionsTask;

        var failures = new[] { customers.IsSuccess, accounts.IsSuccess, payments.IsSuccess, failedPayments.IsSuccess, recentTransactions.IsSuccess }
            .Count(success => !success);

        if (failures > 0)
        {
            var firstError = new[] { customers.ErrorMessage, accounts.ErrorMessage, payments.ErrorMessage, failedPayments.ErrorMessage, recentTransactions.ErrorMessage }
                .FirstOrDefault(e => e is not null);

            return ApiResult<DashboardSummary>.Failure(
                firstError ?? "Das Dashboard konnte nicht vollständig geladen werden.", statusCode: null);
        }

        var summary = new DashboardSummary(
            customers.Value!.TotalCount,
            accounts.Value!.TotalCount,
            payments.Value!.TotalCount,
            failedPayments.Value!.TotalCount,
            recentTransactions.Value!.Items);

        return ApiResult<DashboardSummary>.Success(summary);
    }
}
