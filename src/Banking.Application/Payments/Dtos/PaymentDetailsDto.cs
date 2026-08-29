using Banking.Domain.Accounts;
using Banking.Domain.Payments;

namespace Banking.Application.Payments.Dtos;

public sealed record PaymentDetailsDto(
    PaymentId Id,
    AccountId SourceAccountId,
    string TargetAccountNumber,
    decimal Amount,
    string Currency,
    string Reference,
    PaymentStatus Status,
    DateTime CreatedAt);
