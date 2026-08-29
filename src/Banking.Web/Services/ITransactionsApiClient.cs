using Banking.Web.Models;

namespace Banking.Web.Services;

public interface ITransactionsApiClient
{
    public Task<ApiResult<PagedResult<TransactionSummary>>> SearchAsync(
        Guid? accountId,
        DateTime? from,
        DateTime? to,
        TransactionStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    public Task<ApiResult<TransactionDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
