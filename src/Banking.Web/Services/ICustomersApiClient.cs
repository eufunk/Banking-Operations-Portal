using Banking.Web.Models;

namespace Banking.Web.Services;

public interface ICustomersApiClient
{
    public Task<ApiResult<PagedResult<CustomerSummary>>> SearchAsync(
        string? searchTerm, CustomerStatus? status, int page, int pageSize, CancellationToken cancellationToken);

    public Task<ApiResult<CustomerDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
