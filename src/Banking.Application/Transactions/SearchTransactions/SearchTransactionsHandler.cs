using Banking.Application.Accounts;
using Banking.Application.Common;
using Banking.Application.Common.Errors;
using Banking.Application.Common.Messaging;
using Banking.Application.Common.Pagination;
using Banking.Application.Transactions.Dtos;
using FluentValidation;

namespace Banking.Application.Transactions.SearchTransactions;

public sealed class SearchTransactionsHandler : IQueryHandler<SearchTransactionsQuery, PagedResult<TransactionSummaryDto>>
{
    private readonly IValidator<SearchTransactionsQuery> _validator;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;

    public SearchTransactionsHandler(
        IValidator<SearchTransactionsQuery> validator,
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository)
    {
        _validator = validator;
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<PagedResult<TransactionSummaryDto>>> Handle(SearchTransactionsQuery query, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<PagedResult<TransactionSummaryDto>>.Failure(Error.Validation(
                "Transactions.Search.Invalid",
                "Die Suchparameter sind ungültig.",
                validationResult.Errors.Select(e => e.ErrorMessage).ToArray()));
        }

        if (query.AccountId is not null)
        {
            var account = await _accountRepository.GetByIdAsync(query.AccountId.Value, cancellationToken);
            if (account is null)
            {
                return Result<PagedResult<TransactionSummaryDto>>.Failure(Error.NotFound(
                    "Account.NotFound", $"Konto {query.AccountId} wurde nicht gefunden."));
            }
        }

        var transactions = await _transactionRepository.SearchAsync(
            query.AccountId, query.From, query.To, query.Status, query.Page, query.PageSize, cancellationToken);

        var items = transactions.Items
            .Select(t => new TransactionSummaryDto(t.Id, t.Amount.Amount, t.Amount.Currency.Value, t.BookingDate, t.TransactionType, t.Status, t.Description))
            .ToList();

        return Result<PagedResult<TransactionSummaryDto>>.Success(
            new PagedResult<TransactionSummaryDto>(items, transactions.TotalCount, transactions.Page, transactions.PageSize));
    }
}
