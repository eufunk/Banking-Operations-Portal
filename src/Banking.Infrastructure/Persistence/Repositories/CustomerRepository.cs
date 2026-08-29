using Banking.Application.Common.Pagination;
using Banking.Application.Customers;
using Banking.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Persistence.Repositories;

internal sealed class CustomerRepository : ICustomerRepository
{
    private readonly BankingDbContext _dbContext;

    public CustomerRepository(BankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken)
        => _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<PagedResult<Customer>> SearchAsync(
        string? searchTerm,
        CustomerStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm.Trim()}%";

            // Value Objects mit einfachem HasConversion(e => e.Value, ...) sind hier
            // übersetzbar: EF Core erkennt den Zugriff auf .Value als reinen
            // Spaltenzugriff und übersetzt ihn nach SQL, statt clientseitig auszuwerten.
            query = query.Where(c =>
                EF.Functions.Like(c.FirstName, pattern) ||
                EF.Functions.Like(c.LastName, pattern) ||
                EF.Functions.Like(c.Email.Value, pattern) ||
                EF.Functions.Like(c.CustomerNumber.Value, pattern));
        }

        if (status is not null)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Customer>(items, totalCount, page, pageSize);
    }
}
