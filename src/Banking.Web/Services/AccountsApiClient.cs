using Banking.Web.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace Banking.Web.Services;

public sealed class AccountsApiClient : ApiClientBase, IAccountsApiClient
{
    public AccountsApiClient(HttpClient httpClient, ILogger<AccountsApiClient> logger)
        : base(httpClient, logger)
    {
    }

    public Task<ApiResult<PagedResult<AccountDetailsDto>>> SearchAsync(
        Guid? customerId, AccountStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        };

        if (customerId is not null)
        {
            query["customerId"] = customerId.Value.ToString();
        }

        if (status is not null)
        {
            query["status"] = status.ToString()!;
        }

        var uri = QueryHelpers.AddQueryString("api/accounts", query);
        return GetAsync<PagedResult<AccountDetailsDto>>(uri, cancellationToken);
    }

    public Task<ApiResult<AccountDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetAsync<AccountDetailsDto>($"api/accounts/{id}", cancellationToken);
}
