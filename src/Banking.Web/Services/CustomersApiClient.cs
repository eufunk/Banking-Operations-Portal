using Banking.Web.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace Banking.Web.Services;

public sealed class CustomersApiClient : ApiClientBase, ICustomersApiClient
{
    public CustomersApiClient(HttpClient httpClient, ILogger<CustomersApiClient> logger)
        : base(httpClient, logger)
    {
    }

    public Task<ApiResult<PagedResult<CustomerSummary>>> SearchAsync(
        string? searchTerm, CustomerStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        };

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query["searchTerm"] = searchTerm;
        }

        if (status is not null)
        {
            query["status"] = status.ToString()!;
        }

        var uri = QueryHelpers.AddQueryString("api/customers", query);
        return GetAsync<PagedResult<CustomerSummary>>(uri, cancellationToken);
    }

    public Task<ApiResult<CustomerDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetAsync<CustomerDetailsDto>($"api/customers/{id}", cancellationToken);
}
