using Banking.Application.Common.Pagination;
using Banking.Domain.Accounts;
using Banking.Domain.Transactions;

namespace Banking.Application.Transactions;

public interface ITransactionRepository
{
    public Task<Transaction?> GetByIdAsync(TransactionId id, CancellationToken cancellationToken);

    public Task<PagedResult<Transaction>> SearchByAccountAsync(
        AccountId accountId,
        DateTime? from,
        DateTime? to,
        TransactionStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
