using Banking.Web.Models;

namespace Banking.Web.Services;

public interface IPaymentsApiClient
{
    public Task<ApiResult<PagedResult<PaymentDetails>>> SearchAsync(
        Guid? sourceAccountId, PaymentStatus? status, int page, int pageSize, CancellationToken cancellationToken);

    public Task<ApiResult<CreatePaymentResult>> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken);

    public Task<ApiResult<PaymentDetails>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
