namespace Banking.Web.Models;

public sealed record AccountDetailsDto(
    Guid Id,
    string AccountNumber,
    Guid CustomerId,
    AccountType AccountType,
    string Currency,
    decimal Balance,
    AccountStatus Status);
