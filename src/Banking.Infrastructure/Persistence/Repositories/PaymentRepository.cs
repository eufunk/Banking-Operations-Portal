using Banking.Application.Payments;
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

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken)
        => await _dbContext.Payments.AddAsync(payment, cancellationToken);
}
