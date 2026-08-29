namespace Banking.Web.Models;

public sealed record TransactionSummary(
    Guid Id,
    decimal Amount,
    string Currency,
    DateTime BookingDate,
    TransactionType TransactionType,
    TransactionStatus Status,
    string? Description);

public sealed record TransactionDetailsDto(
    Guid Id,
    Guid AccountId,
    decimal Amount,
    string Currency,
    DateTime BookingDate,
    TransactionType TransactionType,
    TransactionStatus Status,
    string? Description);
