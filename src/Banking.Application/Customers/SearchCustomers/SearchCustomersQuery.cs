using Banking.Application.Common.Messaging;
using Banking.Application.Common.Pagination;
using Banking.Application.Customers.Dtos;
using Banking.Domain.Customers;

namespace Banking.Application.Customers.SearchCustomers;

public sealed record SearchCustomersQuery(
    string? SearchTerm,
    CustomerStatus? Status,
    int Page,
    int PageSize) : IQuery<PagedResult<CustomerSummaryDto>>;
