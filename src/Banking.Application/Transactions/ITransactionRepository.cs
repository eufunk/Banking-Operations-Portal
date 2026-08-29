using Banking.Application.Common.Pagination;
using Banking.Domain.Accounts;
using Banking.Domain.Transactions;

namespace Banking.Application.Transactions;

public interface ITransactionRepository
{
    public Task<Transaction?> GetByIdAsync(TransactionId id, CancellationToken cancellationToken);

    /// <summary>accountId == null -> neueste Transaktionen über alle Konten hinweg.</summary>
    public Task<PagedResult<Transaction>> SearchAsync(
        AccountId? accountId,
        DateTime? from,
        DateTime? to,
        TransactionStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
