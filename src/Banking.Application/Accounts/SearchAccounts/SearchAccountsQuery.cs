using Banking.Application.Accounts.Dtos;
using Banking.Application.Common.Messaging;
using Banking.Application.Common.Pagination;
using Banking.Domain.Accounts;
using Banking.Domain.Customers;

namespace Banking.Application.Accounts.SearchAccounts;

public sealed record SearchAccountsQuery(
    CustomerId? CustomerId,
    AccountStatus? Status,
    int Page,
    int PageSize) : IQuery<PagedResult<AccountDetailsDto>>;
