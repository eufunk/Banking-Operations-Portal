using Banking.Domain.Customers;

namespace Banking.Application.Customers.Dtos;

public sealed record CustomerSummaryDto(
    CustomerId Id,
    string CustomerNumber,
    string FullName,
    string Email,
    CustomerStatus Status);
