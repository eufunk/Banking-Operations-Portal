using Banking.Application.Common.Pagination;
using Banking.Domain.Accounts;
using Banking.Domain.Payments;

namespace Banking.Application.Payments;

public interface IPaymentRepository
{
    public Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken);

    /// <summary>Schlanke Projektion für Statusabfragen - lädt nicht das komplette Aggregat.</summary>
    public Task<PaymentStatus?> GetStatusAsync(PaymentId id, CancellationToken cancellationToken);

    public Task<PagedResult<Payment>> SearchAsync(
        AccountId? sourceAccountId,
        PaymentStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    public Task AddAsync(Payment payment, CancellationToken cancellationToken);
}
