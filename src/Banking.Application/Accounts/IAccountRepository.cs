using Banking.Application.Common.Pagination;
using Banking.Domain.Accounts;
using Banking.Domain.Customers;

namespace Banking.Application.Accounts;

public interface IAccountRepository
{
    public Task<Account?> GetByIdAsync(AccountId id, CancellationToken cancellationToken);

    public Task<Account?> GetByAccountNumberAsync(AccountNumber accountNumber, CancellationToken cancellationToken);

    public Task<PagedResult<Account>> SearchAsync(
        CustomerId? customerId,
        AccountStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
