using Banking.Web.Models;

namespace Banking.Web.Services;

public interface IAccountsApiClient
{
    public Task<ApiResult<PagedResult<AccountDetailsDto>>> SearchAsync(
        Guid? customerId, AccountStatus? status, int page, int pageSize, CancellationToken cancellationToken);

    public Task<ApiResult<AccountDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
