using Banking.Application.Accounts.Dtos;
using Banking.Application.Common;
using Banking.Application.Common.Errors;
using Banking.Application.Common.Messaging;
using Banking.Application.Common.Pagination;
using FluentValidation;

namespace Banking.Application.Accounts.SearchAccounts;

public sealed class SearchAccountsHandler : IQueryHandler<SearchAccountsQuery, PagedResult<AccountDetailsDto>>
{
    private readonly IValidator<SearchAccountsQuery> _validator;
    private readonly IAccountRepository _accountRepository;

    public SearchAccountsHandler(IValidator<SearchAccountsQuery> validator, IAccountRepository accountRepository)
    {
        _validator = validator;
        _accountRepository = accountRepository;
    }

    public async Task<Result<PagedResult<AccountDetailsDto>>> Handle(SearchAccountsQuery query, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<PagedResult<AccountDetailsDto>>.Failure(Error.Validation(
                "Accounts.Search.Invalid",
                "Die Suchparameter sind ungültig.",
                validationResult.Errors.Select(e => e.ErrorMessage).ToArray()));
        }

        var accounts = await _accountRepository.SearchAsync(
            query.CustomerId, query.Status, query.Page, query.PageSize, cancellationToken);

        var items = accounts.Items
            .Select(a => new AccountDetailsDto(a.Id, a.AccountNumber.Value, a.CustomerId, a.AccountType, a.Currency.Value, a.Balance.Amount, a.Status))
            .ToList();

        return Result<PagedResult<AccountDetailsDto>>.Success(
            new PagedResult<AccountDetailsDto>(items, accounts.TotalCount, accounts.Page, accounts.PageSize));
    }
}
