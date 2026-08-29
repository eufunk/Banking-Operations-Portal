namespace Banking.Web.Models;

public sealed record CustomerSummary(Guid Id, string CustomerNumber, string FullName, string Email, CustomerStatus Status);

public sealed record CustomerDetailsDto(
    Guid Id,
    string CustomerNumber,
    string FirstName,
    string LastName,
    string Email,
    CustomerStatus Status,
    DateTime CreatedAt);
