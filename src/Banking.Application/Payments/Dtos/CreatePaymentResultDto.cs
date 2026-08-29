using Banking.Domain.Payments;

namespace Banking.Application.Payments.Dtos;

public sealed record CreatePaymentResultDto(PaymentId Id, PaymentStatus Status);
