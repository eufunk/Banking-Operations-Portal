using Banking.Application.Common.Messaging;
using Banking.Application.Common.Pagination;
using Banking.Application.Payments.Dtos;
using Banking.Domain.Accounts;
using Banking.Domain.Payments;

namespace Banking.Application.Payments.SearchPayments;

public sealed record SearchPaymentsQuery(
    AccountId? SourceAccountId,
    PaymentStatus? Status,
    int Page,
    int PageSize) : IQuery<PagedResult<PaymentDetailsDto>>;
