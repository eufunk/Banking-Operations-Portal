using Banking.Application.Common;
using Banking.Application.Common.Errors;
using Banking.Application.Common.Messaging;
using Banking.Application.Payments.Dtos;

namespace Banking.Application.Payments.GetPayment;

public sealed class GetPaymentHandler : IQueryHandler<GetPaymentQuery, PaymentDetailsDto>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<PaymentDetailsDto>> Handle(GetPaymentQuery query, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(query.PaymentId, cancellationToken);
        if (payment is null)
        {
            return Result<PaymentDetailsDto>.Failure(Error.NotFound(
                "Payment.NotFound", $"Zahlungsauftrag {query.PaymentId} wurde nicht gefunden."));
        }

        var dto = new PaymentDetailsDto(
            payment.Id,
            payment.SourceAccountId,
            payment.TargetAccountNumber.Value,
            payment.Amount.Amount,
            payment.Amount.Currency.Value,
            payment.Reference.Value,
            payment.Status,
            payment.CreatedAt);

        return Result<PaymentDetailsDto>.Success(dto);
    }
}
