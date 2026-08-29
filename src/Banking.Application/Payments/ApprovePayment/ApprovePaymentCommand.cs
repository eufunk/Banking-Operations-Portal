using Banking.Application.Common.Messaging;
using Banking.Application.Payments.Dtos;
using Banking.Domain.Payments;

namespace Banking.Application.Payments.ApprovePayment;

public sealed record ApprovePaymentCommand(PaymentId PaymentId) : ICommand<PaymentStatusDto>;
