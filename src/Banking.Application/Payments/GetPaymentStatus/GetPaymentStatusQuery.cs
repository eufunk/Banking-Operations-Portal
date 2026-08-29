using Banking.Application.Common.Messaging;
using Banking.Application.Payments.Dtos;
using Banking.Domain.Payments;

namespace Banking.Application.Payments.GetPaymentStatus;

public sealed record GetPaymentStatusQuery(PaymentId PaymentId) : IQuery<PaymentStatusDto>;
