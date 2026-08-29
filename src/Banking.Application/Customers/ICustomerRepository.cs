using Banking.Application.Common.Pagination;
using Banking.Domain.Customers;

namespace Banking.Application.Customers;

public interface ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken);

    public Task<PagedResult<Customer>> SearchAsync(
        string? searchTerm,
        CustomerStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
