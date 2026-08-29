using Banking.Application.Common.Messaging;
using Banking.Application.Payments.Dtos;
using Banking.Domain.Payments;

namespace Banking.Application.Payments.RejectPayment;

public sealed record RejectPaymentCommand(PaymentId PaymentId) : ICommand<PaymentStatusDto>;
