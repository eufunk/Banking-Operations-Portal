using Banking.Domain.Accounts;

namespace Banking.Application.Accounts;

public interface IAccountRepository
{
    public Task<Account?> GetByIdAsync(AccountId id, CancellationToken cancellationToken);
}
