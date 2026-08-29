using Banking.Domain.Transactions;

namespace Banking.Application.Transactions.Dtos;

public sealed record TransactionSummaryDto(
    TransactionId Id,
    decimal Amount,
    string Currency,
    DateTime BookingDate,
    TransactionType TransactionType,
    TransactionStatus Status,
    string? Description);
