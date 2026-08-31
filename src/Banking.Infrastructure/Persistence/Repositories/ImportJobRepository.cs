using Banking.Application.Common.Pagination;
using Banking.Application.Imports;
using Banking.Domain.Imports;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Persistence.Repositories;

internal sealed class ImportJobRepository : IImportJobRepository
{
    private readonly BankingDbContext _dbContext;

    public ImportJobRepository(BankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ImportJob importJob, CancellationToken cancellationToken)
        => await _dbContext.ImportJobs.AddAsync(importJob, cancellationToken);

    public Task<ImportJob?> GetByIdAsync(ImportJobId id, CancellationToken cancellationToken)
        => _dbContext.ImportJobs
            .Include(j => j.Records)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public async Task<PagedResult<ImportJob>> SearchAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _dbContext.ImportJobs.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(j => j.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ImportJob>(items, totalCount, page, pageSize);
    }

    public Task<bool> ExistsSuccessfulRecordAsync(string externalTransactionId, CancellationToken cancellationToken)
        => _dbContext.Set<ImportRecord>()
            .AnyAsync(r => r.ExternalTransactionId == externalTransactionId && r.Outcome == ImportRecordOutcome.Success, cancellationToken);
}
