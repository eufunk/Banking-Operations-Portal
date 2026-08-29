using Banking.Domain.Accounts;
using Banking.Domain.Customers;

namespace Banking.Application.Accounts.Dtos;

public sealed record AccountDetailsDto(
    AccountId Id,
    string AccountNumber,
    CustomerId CustomerId,
    AccountType AccountType,
    string Currency,
    decimal Balance,
    AccountStatus Status);
