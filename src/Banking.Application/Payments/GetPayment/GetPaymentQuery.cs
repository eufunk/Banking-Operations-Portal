using Banking.Application.Common.Messaging;
using Banking.Application.Payments.Dtos;
using Banking.Domain.Payments;

namespace Banking.Application.Payments.GetPayment;

public sealed record GetPaymentQuery(PaymentId PaymentId) : IQuery<PaymentDetailsDto>;
