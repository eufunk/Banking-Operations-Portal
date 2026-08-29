using Banking.Application.Common.Messaging;
using Banking.Application.Common.Pagination;
using Banking.Application.Transactions.Dtos;
using Banking.Domain.Accounts;
using Banking.Domain.Transactions;

namespace Banking.Application.Transactions.SearchTransactions;

/// <summary>
/// AccountId ist bewusst optional: ohne Angabe liefert die Suche die neuesten
/// Transaktionen über alle Konten hinweg (z. B. für das Dashboard), mit Angabe den
/// Kontoauszug eines einzelnen Kontos.
/// </summary>
public sealed record SearchTransactionsQuery(
    AccountId? AccountId,
    DateTime? From,
    DateTime? To,
    TransactionStatus? Status,
    int Page,
    int PageSize) : IQuery<PagedResult<TransactionSummaryDto>>;
