using Banking.Domain.Payments;

namespace Banking.Application.Payments.Dtos;

public sealed record PaymentStatusDto(PaymentId Id, PaymentStatus Status);
