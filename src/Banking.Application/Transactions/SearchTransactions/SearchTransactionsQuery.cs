using Banking.Application.Common.Messaging;
using Banking.Application.Common.Pagination;
using Banking.Application.Transactions.Dtos;
using Banking.Domain.Accounts;
using Banking.Domain.Transactions;

namespace Banking.Application.Transactions.SearchTransactions;

public sealed record SearchTransactionsQuery(
    AccountId AccountId,
    DateTime? From,
    DateTime? To,
    TransactionStatus? Status,
    int Page,
    int PageSize) : IQuery<PagedResult<TransactionSummaryDto>>;
