using System.Globalization;
using Banking.Web.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace Banking.Web.Services;

public sealed class TransactionsApiClient : ApiClientBase, ITransactionsApiClient
{
    public TransactionsApiClient(HttpClient httpClient, ILogger<TransactionsApiClient> logger)
        : base(httpClient, logger)
    {
    }

    public Task<ApiResult<PagedResult<TransactionSummary>>> SearchAsync(
        Guid? accountId,
        DateTime? from,
        DateTime? to,
        TransactionStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        };

        if (accountId is not null)
        {
            query["accountId"] = accountId.Value.ToString();
        }

        if (from is not null)
        {
            query["from"] = from.Value.ToString("O", CultureInfo.InvariantCulture);
        }

        if (to is not null)
        {
            query["to"] = to.Value.ToString("O", CultureInfo.InvariantCulture);
        }

        if (status is not null)
        {
            query["status"] = status.ToString()!;
        }

        var uri = QueryHelpers.AddQueryString("api/transactions", query);
        return GetAsync<PagedResult<TransactionSummary>>(uri, cancellationToken);
    }

    public Task<ApiResult<TransactionDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetAsync<TransactionDetailsDto>($"api/transactions/{id}", cancellationToken);
}
