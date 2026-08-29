using Banking.Application.Common;
using Banking.Application.Common.Errors;
using Banking.Application.Common.Messaging;
using Banking.Application.Payments.Dtos;

namespace Banking.Application.Payments.GetPaymentStatus;

public sealed class GetPaymentStatusHandler : IQueryHandler<GetPaymentStatusQuery, PaymentStatusDto>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentStatusHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<PaymentStatusDto>> Handle(GetPaymentStatusQuery query, CancellationToken cancellationToken)
    {
        var status = await _paymentRepository.GetStatusAsync(query.PaymentId, cancellationToken);
        if (status is null)
        {
            return Result<PaymentStatusDto>.Failure(Error.NotFound(
                "Payment.NotFound", $"Zahlungsauftrag {query.PaymentId} wurde nicht gefunden."));
        }

        return Result<PaymentStatusDto>.Success(new PaymentStatusDto(query.PaymentId, status.Value));
    }
}
