using Banking.Application.Common.Pagination;
using Banking.Domain.Accounts;
using Banking.Domain.Common;
using Banking.Domain.Payments;

namespace Banking.Application.Payments;

public interface IPaymentRepository
{
    public Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken);

    /// <summary>
    /// Schutz vor versehentlicher Doppel-Einreichung (z. B. Doppelklick) - keine
    /// umfassende Betrugserkennung, nur ein Zeitfenster-Check auf exakt gleiche Eckdaten,
    /// die noch nicht abgelehnt/fehlgeschlagen sind.
    /// </summary>
    public Task<bool> ExistsSimilarRecentAsync(
        AccountId sourceAccountId,
        AccountNumber targetAccountNumber,
        Money amount,
        DateTime since,
        CancellationToken cancellationToken);

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
