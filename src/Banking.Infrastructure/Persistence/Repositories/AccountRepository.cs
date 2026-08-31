using Banking.Application.Accounts;
using Banking.Application.Common.Pagination;
using Banking.Domain.Accounts;
using Banking.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Persistence.Repositories;

internal sealed class AccountRepository : IAccountRepository
{
    private readonly BankingDbContext _dbContext;

    public AccountRepository(BankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Account?> GetByIdAsync(AccountId id, CancellationToken cancellationToken)
        => _dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<Account?> GetByAccountNumberAsync(AccountNumber accountNumber, CancellationToken cancellationToken)
        => _dbContext.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, cancellationToken);

    public async Task<PagedResult<Account>> SearchAsync(
        CustomerId? customerId,
        AccountStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Accounts.AsNoTracking().AsQueryable();

        if (customerId is not null)
        {
            query = query.Where(a => a.CustomerId == customerId.Value);
        }

        if (status is not null)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(a => a.AccountNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Account>(items, totalCount, page, pageSize);
    }
}
