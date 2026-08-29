using Banking.Application.Accounts;
using Banking.Application.Common;
using Banking.Application.Common.Errors;
using Banking.Application.Common.Messaging;
using Banking.Application.Payments.Dtos;
using Banking.Domain.Accounts;
using Banking.Domain.Common;
using Banking.Domain.Payments;
using FluentValidation;

namespace Banking.Application.Payments.CreatePayment;

public sealed class CreatePaymentHandler : ICommandHandler<CreatePaymentCommand, CreatePaymentResultDto>
{
    private readonly IValidator<CreatePaymentCommand> _validator;
    private readonly IAccountRepository _accountRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePaymentHandler(
        IValidator<CreatePaymentCommand> validator,
        IAccountRepository accountRepository,
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _accountRepository = accountRepository;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreatePaymentResultDto>> Handle(CreatePaymentCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<CreatePaymentResultDto>.Failure(Error.Validation(
                "Payment.Invalid",
                "Der Zahlungsauftrag enthält ungültige Daten.",
                validationResult.Errors.Select(e => e.ErrorMessage).ToArray()));
        }

        var sourceAccount = await _accountRepository.GetByIdAsync(command.SourceAccountId, cancellationToken);
        if (sourceAccount is null)
        {
            return Result<CreatePaymentResultDto>.Failure(Error.NotFound(
                "Account.NotFound", $"Konto {command.SourceAccountId} wurde nicht gefunden."));
        }

        if (sourceAccount.Status != AccountStatus.Active)
        {
            return Result<CreatePaymentResultDto>.Failure(Error.Conflict(
                "Account.NotActive", $"Konto {sourceAccount.AccountNumber} ist nicht aktiv (Status: {sourceAccount.Status})."));
        }

        // Fachliche Regel auf Application-Ebene, nicht im Domain-Modell: Payment kennt
        // Account gar nicht (getrennte Aggregate), daher kann nur der Handler - der beide
        // lädt - prüfen, ob Zahlungswährung und Kontowährung zusammenpassen.
        CurrencyCode currency;
        try
        {
            currency = new CurrencyCode(command.Currency);
        }
        catch (ArgumentException ex)
        {
            return Result<CreatePaymentResultDto>.Failure(Error.Validation("Payment.InvalidCurrency", ex.Message));
        }

        if (!currency.Equals(sourceAccount.Currency))
        {
            return Result<CreatePaymentResultDto>.Failure(Error.Conflict(
                "Payment.CurrencyMismatch",
                $"Zahlungswährung {currency} passt nicht zur Kontowährung {sourceAccount.Currency} von Konto {sourceAccount.AccountNumber}."));
        }

        Payment payment;
        try
        {
            payment = Payment.Create(
                command.SourceAccountId,
                new AccountNumber(command.TargetAccountNumber),
                Money.Positive(command.Amount, currency),
                new PaymentReference(command.Reference));
        }
        catch (ArgumentException ex)
        {
            // Value-Object-Guards greifen hier nur noch als letzte Verteidigungslinie -
            // der Validator oben hat dieselben Fälle bereits abgefangen.
            return Result<CreatePaymentResultDto>.Failure(Error.Validation("Payment.Invalid", ex.Message));
        }

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CreatePaymentResultDto>.Success(new CreatePaymentResultDto(payment.Id, payment.Status));
    }
}
