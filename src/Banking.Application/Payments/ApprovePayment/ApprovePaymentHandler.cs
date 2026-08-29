using Banking.Application.Common;
using Banking.Application.Common.Errors;
using Banking.Application.Common.Messaging;
using Banking.Application.Payments.Dtos;
using Banking.Domain.Common.Exceptions;

namespace Banking.Application.Payments.ApprovePayment;

public sealed class ApprovePaymentHandler : ICommandHandler<ApprovePaymentCommand, PaymentStatusDto>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ApprovePaymentHandler(IPaymentRepository paymentRepository, IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PaymentStatusDto>> Handle(ApprovePaymentCommand command, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(command.PaymentId, cancellationToken);
        if (payment is null)
        {
            return Result<PaymentStatusDto>.Failure(Error.NotFound(
                "Payment.NotFound", $"Zahlungsauftrag {command.PaymentId} wurde nicht gefunden."));
        }

        try
        {
            payment.Approve();
        }
        catch (InvalidStatusTransitionException ex)
        {
            return Result<PaymentStatusDto>.Failure(Error.Conflict("Payment.InvalidTransition", ex.Message));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PaymentStatusDto>.Success(new PaymentStatusDto(payment.Id, payment.Status));
    }
}
