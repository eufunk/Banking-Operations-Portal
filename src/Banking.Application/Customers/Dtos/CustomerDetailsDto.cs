using Banking.Domain.Customers;

namespace Banking.Application.Customers.Dtos;

public sealed record CustomerDetailsDto(
    CustomerId Id,
    string CustomerNumber,
    string FirstName,
    string LastName,
    string Email,
    CustomerStatus Status,
    DateTime CreatedAt);
