using Banking.Application.Accounts.Dtos;
using Banking.Application.Common;
using Banking.Application.Common.Errors;
using Banking.Application.Common.Messaging;

namespace Banking.Application.Accounts.GetAccountDetails;

public sealed class GetAccountDetailsHandler : IQueryHandler<GetAccountDetailsQuery, AccountDetailsDto>
{
    private readonly IAccountRepository _accountRepository;

    public GetAccountDetailsHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Result<AccountDetailsDto>> Handle(GetAccountDetailsQuery query, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(query.AccountId, cancellationToken);
        if (account is null)
        {
            return Result<AccountDetailsDto>.Failure(Error.NotFound(
                "Account.NotFound", $"Konto {query.AccountId} wurde nicht gefunden."));
        }

        var dto = new AccountDetailsDto(
            account.Id,
            account.AccountNumber.Value,
            account.CustomerId,
            account.AccountType,
            account.Currency.Value,
            account.Balance.Amount,
            account.Status);

        return Result<AccountDetailsDto>.Success(dto);
    }
}
