using Banking.Application.Common.Messaging;
using Banking.Application.Payments.Dtos;
using Banking.Domain.Accounts;

namespace Banking.Application.Payments.CreatePayment;

public sealed record CreatePaymentCommand(
    AccountId SourceAccountId,
    string TargetAccountNumber,
    decimal Amount,
    string Currency,
    string Reference) : ICommand<CreatePaymentResultDto>;
