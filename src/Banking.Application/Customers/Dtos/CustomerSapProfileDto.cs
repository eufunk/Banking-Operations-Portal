using Banking.Domain.Accounts;
using Banking.Domain.Customers;

namespace Banking.Application.Customers.Dtos;

/// <summary>
/// "Customer 360"-Ansicht: lokale Stammdaten + SAP-Anreicherung. <see cref="SapAvailable"/>
/// ist bewusst von den SAP-Feldern selbst getrennt - so kann die UI zwischen "SAP hat keine
/// Daten zu diesem Kunden" (fachlich, <see cref="SapProfile"/> ist <c>null</c> trotz
/// <c>SapAvailable = true</c>) und "SAP war gerade nicht erreichbar" (technisch,
/// <c>SapAvailable = false</c>) unterscheiden.
/// </summary>
public sealed record CustomerSapProfileDto(
    CustomerId Id,
    string CustomerNumber,
    string FirstName,
    string LastName,
    string Email,
    CustomerStatus Status,
    bool SapAvailable,
    SapCustomerProfileSectionDto? SapProfile,
    IReadOnlyList<CustomerSapAccountDto> Accounts);

public sealed record SapCustomerProfileSectionDto(
    string TaxId,
    string Street,
    string PostalCode,
    string City,
    string Country,
    string RiskRating,
    DateTimeOffset SyncedAt);

public sealed record CustomerSapAccountDto(
    AccountId Id,
    string AccountNumber,
    AccountType AccountType,
    string Currency,
    decimal Balance,
    AccountStatus Status,
    string? SapAccountStatus,
    bool? SapIbanVerified);
