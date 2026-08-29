using Banking.Application.Common;

namespace Banking.Infrastructure.Persistence;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly BankingDbContext _dbContext;

    public UnitOfWork(BankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
