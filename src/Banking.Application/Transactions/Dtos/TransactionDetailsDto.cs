using Banking.Domain.Accounts;
using Banking.Domain.Transactions;

namespace Banking.Application.Transactions.Dtos;

public sealed record TransactionDetailsDto(
    TransactionId Id,
    AccountId AccountId,
    decimal Amount,
    string Currency,
    DateTime BookingDate,
    TransactionType TransactionType,
    TransactionStatus Status,
    string? Description);
