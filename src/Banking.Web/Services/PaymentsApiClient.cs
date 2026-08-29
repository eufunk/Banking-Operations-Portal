using Banking.Web.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace Banking.Web.Services;

public sealed class PaymentsApiClient : ApiClientBase, IPaymentsApiClient
{
    public PaymentsApiClient(HttpClient httpClient, ILogger<PaymentsApiClient> logger)
        : base(httpClient, logger)
    {
    }

    public Task<ApiResult<PagedResult<PaymentDetails>>> SearchAsync(
        Guid? sourceAccountId, PaymentStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        };

        if (sourceAccountId is not null)
        {
            query["sourceAccountId"] = sourceAccountId.Value.ToString();
        }

        if (status is not null)
        {
            query["status"] = status.ToString()!;
        }

        var uri = QueryHelpers.AddQueryString("api/payments", query);
        return GetAsync<PagedResult<PaymentDetails>>(uri, cancellationToken);
    }

    public Task<ApiResult<CreatePaymentResult>> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken)
        => PostAsync<CreatePaymentRequest, CreatePaymentResult>("api/payments", request, cancellationToken);

    public Task<ApiResult<PaymentDetails>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetAsync<PaymentDetails>($"api/payments/{id}", cancellationToken);
}
