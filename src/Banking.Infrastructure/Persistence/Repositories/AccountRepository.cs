using Banking.Application.Accounts;
using Banking.Domain.Accounts;
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
}
