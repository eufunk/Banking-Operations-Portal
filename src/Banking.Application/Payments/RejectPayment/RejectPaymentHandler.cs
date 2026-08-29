using Banking.Application.Common;
using Banking.Application.Common.Errors;
using Banking.Application.Common.Messaging;
using Banking.Application.Payments.Dtos;
using Banking.Domain.Common.Exceptions;

namespace Banking.Application.Payments.RejectPayment;

public sealed class RejectPaymentHandler : ICommandHandler<RejectPaymentCommand, PaymentStatusDto>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RejectPaymentHandler(IPaymentRepository paymentRepository, IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PaymentStatusDto>> Handle(RejectPaymentCommand command, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(command.PaymentId, cancellationToken);
        if (payment is null)
        {
            return Result<PaymentStatusDto>.Failure(Error.NotFound(
                "Payment.NotFound", $"Zahlungsauftrag {command.PaymentId} wurde nicht gefunden."));
        }

        try
        {
            payment.Reject();
        }
        catch (InvalidStatusTransitionException ex)
        {
            return Result<PaymentStatusDto>.Failure(Error.Conflict("Payment.InvalidTransition", ex.Message));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PaymentStatusDto>.Success(new PaymentStatusDto(payment.Id, payment.Status));
    }
}
