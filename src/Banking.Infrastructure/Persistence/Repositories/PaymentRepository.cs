using Banking.Application.Common.Pagination;
using Banking.Application.Payments;
using Banking.Domain.Accounts;
using Banking.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Persistence.Repositories;

internal sealed class PaymentRepository : IPaymentRepository
{
    private readonly BankingDbContext _dbContext;

    public PaymentRepository(BankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken)
        => _dbContext.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<PaymentStatus?> GetStatusAsync(PaymentId id, CancellationToken cancellationToken)
        => _dbContext.Payments
            .Where(p => p.Id == id)
            .Select(p => (PaymentStatus?)p.Status)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<Payment>> SearchAsync(
        AccountId? sourceAccountId,
        PaymentStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Payments.AsNoTracking().AsQueryable();

        if (sourceAccountId is not null)
        {
            query = query.Where(p => p.SourceAccountId == sourceAccountId.Value);
        }

        if (status is not null)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Payment>(items, totalCount, page, pageSize);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken)
        => await _dbContext.Payments.AddAsync(payment, cancellationToken);
}
