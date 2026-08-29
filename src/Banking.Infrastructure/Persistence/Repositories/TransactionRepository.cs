using Banking.Application.Common.Pagination;
using Banking.Application.Transactions;
using Banking.Domain.Accounts;
using Banking.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Persistence.Repositories;

internal sealed class TransactionRepository : ITransactionRepository
{
    private readonly BankingDbContext _dbContext;

    public TransactionRepository(BankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Transaction?> GetByIdAsync(TransactionId id, CancellationToken cancellationToken)
        => _dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<PagedResult<Transaction>> SearchAsync(
        AccountId? accountId,
        DateTime? from,
        DateTime? to,
        TransactionStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Transactions.AsNoTracking().AsQueryable();

        if (accountId is not null)
        {
            // Nutzt den Index IX_Transactions_AccountId_BookingDate (Filter + Sortierung).
            query = query.Where(t => t.AccountId == accountId.Value);
        }

        if (from is not null)
        {
            query = query.Where(t => t.BookingDate >= from.Value);
        }

        if (to is not null)
        {
            query = query.Where(t => t.BookingDate <= to.Value);
        }

        if (status is not null)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.BookingDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Transaction>(items, totalCount, page, pageSize);
    }
}
