using System.Diagnostics;
using Banking.Application.Accounts;
using Banking.Application.Common;
using Banking.Application.Common.Errors;
using Banking.Application.Common.Messaging;
using Banking.Application.Common.Options;
using Banking.Application.Customers;
using Banking.Application.Payments.Dtos;
using Banking.Domain.Accounts;
using Banking.Domain.Common;
using Banking.Domain.Customers;
using Banking.Domain.Payments;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Banking.Application.Payments.CreatePayment;

public sealed class CreatePaymentHandler : ICommandHandler<CreatePaymentCommand, CreatePaymentResultDto>
{
    // Schutz vor versehentlicher Doppel-Einreichung (z. B. Doppelklick) - bewusst kurz und
    // fest codiert, keine allgemeine Betrugserkennung. Ein längeres Fenster würde legitime,
    // tatsächlich wiederholte Zahlungen (z. B. zweimal dieselbe Miete in unterschiedlichen
    // Monaten mit identischem Betrag) fälschlich blockieren.
    private static readonly TimeSpan DuplicateCheckWindow = TimeSpan.FromMinutes(1);

    private readonly IValidator<CreatePaymentCommand> _validator;
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOptionsMonitor<FeatureFlagsOptions> _featureFlags;

    public CreatePaymentHandler(
        IValidator<CreatePaymentCommand> validator,
        IAccountRepository accountRepository,
        ICustomerRepository customerRepository,
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork,
        IOptionsMonitor<FeatureFlagsOptions> featureFlags)
    {
        _validator = validator;
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _featureFlags = featureFlags;
    }

    public async Task<Result<CreatePaymentResultDto>> Handle(CreatePaymentCommand command, CancellationToken cancellationToken)
    {
        // Eigene Activity ("Span") für den fachlichen Schritt "Payment Application Service"
        // in der Kette POST /api/payments -> Application Service -> SQL -> (SAP, Kapitel 11)
        // -> Payment completed. Der HTTP-Request-Span und der SQL-Dependency-Span entstehen
        // automatisch über die OpenTelemetry-Instrumentierung in Banking.Api/Program.cs -
        // dieser Span hier schließt die Lücke dazwischen und macht die reine
        // Geschäftslogik-Zeit sichtbar. Ohne registrierten Listener (z. B. lokal ohne
        // Konfiguration) ist StartActivity ein günstiges No-Op.
        using var activity = Telemetry.ActivitySource.StartActivity("PaymentProcessing");
        var stopwatch = Stopwatch.StartNew();

        // Bewusst keine sensiblen Werte (IBAN, Betrag) als Tags/Logs - nur die Konto-Id,
        // die ohne zusätzlichen Datenbankzugriff niemandem etwas verrät.
        activity?.SetTag("payment.source_account_id", command.SourceAccountId.Value);

        Result<CreatePaymentResultDto> Fail(Error error, string reason)
        {
            stopwatch.Stop();
            Telemetry.PaymentFailedCounter.Add(1, new KeyValuePair<string, object?>("reason", reason));
            Telemetry.PaymentProcessingDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Error, reason);
            activity?.SetTag("payment.failure_reason", reason);
            return Result<CreatePaymentResultDto>.Failure(error);
        }

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Fail(
                Error.Validation(
                    "Payment.Invalid",
                    "Der Zahlungsauftrag enthält ungültige Daten.",
                    validationResult.Errors.Select(e => e.ErrorMessage).ToArray()),
                "validation");
        }

        var sourceAccount = await _accountRepository.GetByIdAsync(command.SourceAccountId, cancellationToken);
        if (sourceAccount is null)
        {
            return Fail(
                Error.NotFound("Account.NotFound", $"Konto {command.SourceAccountId} wurde nicht gefunden."),
                "account_not_found");
        }

        if (sourceAccount.Status != AccountStatus.Active)
        {
            return Fail(
                Error.Conflict("Account.NotActive", $"Konto {sourceAccount.AccountNumber} ist nicht aktiv (Status: {sourceAccount.Status})."),
                "account_not_active");
        }

        // Konto kann aktiv sein, während der zugehörige Kunde es nicht mehr ist (z. B.
        // Kunde wurde nachträglich blockiert) - beide Prüfungen sind unabhängig voneinander nötig.
        var customer = await _customerRepository.GetByIdAsync(sourceAccount.CustomerId, cancellationToken);
        if (customer is null)
        {
            return Fail(
                Error.NotFound("Customer.NotFound", $"Kunde {sourceAccount.CustomerId} wurde nicht gefunden."),
                "customer_not_found");
        }

        if (customer.Status != CustomerStatus.Active)
        {
            return Fail(
                Error.Conflict("Customer.NotActive", $"Kunde {customer.CustomerNumber} ist nicht aktiv (Status: {customer.Status})."),
                "customer_not_active");
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
            return Fail(Error.Validation("Payment.InvalidCurrency", ex.Message), "invalid_currency");
        }

        if (!currency.Equals(sourceAccount.Currency))
        {
            return Fail(
                Error.Conflict(
                    "Payment.CurrencyMismatch",
                    $"Zahlungswährung {currency} passt nicht zur Kontowährung {sourceAccount.Currency} von Konto {sourceAccount.AccountNumber}."),
                "currency_mismatch");
        }

        // Der tatsächliche Kontostand-Abzug passiert erst bei der Ausführung (Account.Debit),
        // nicht beim Anlegen des Auftrags - trotzdem soll ein Sachbearbeiter nicht schon beim
        // Anlegen einen Auftrag erstellen können, für den offensichtlich keine Deckung besteht.
        // Kreditkonten dürfen bewusst ins Minus laufen (siehe Account.Debit), daher hier ausgenommen.
        if (sourceAccount.AccountType != AccountType.Credit && sourceAccount.Balance.Amount < command.Amount)
        {
            return Fail(
                Error.Conflict(
                    "Payment.InsufficientFunds",
                    $"Konto {sourceAccount.AccountNumber} hat mit {sourceAccount.Balance} nicht ausreichend Guthaben für {command.Amount:N2} {currency}."),
                "insufficient_funds");
        }

        AccountNumber targetAccountNumber;
        try
        {
            targetAccountNumber = new AccountNumber(command.TargetAccountNumber);
        }
        catch (ArgumentException ex)
        {
            return Fail(Error.Validation("Payment.InvalidTargetAccount", ex.Message), "invalid_target_account");
        }

        var duplicateSince = DateTime.UtcNow - DuplicateCheckWindow;
        var isDuplicate = await _paymentRepository.ExistsSimilarRecentAsync(
            command.SourceAccountId, targetAccountNumber, new Money(command.Amount, currency), duplicateSince, cancellationToken);
        if (isDuplicate)
        {
            return Fail(
                Error.Conflict("Payment.Duplicate", "Ein identischer Zahlungsauftrag wurde soeben bereits eingereicht."),
                "duplicate_payment");
        }

        Payment payment;
        try
        {
            payment = Payment.Create(
                command.SourceAccountId,
                targetAccountNumber,
                Money.Positive(command.Amount, currency),
                new PaymentReference(command.Reference));
        }
        catch (ArgumentException ex)
        {
            // Value-Object-Guards greifen hier nur noch als letzte Verteidigungslinie -
            // der Validator oben hat dieselben Fälle bereits abgefangen.
            return Fail(Error.Validation("Payment.Invalid", ex.Message), "invalid_payment");
        }

        // Feature Flag statt Code-Änderung: Freigabe-Workflow lässt sich z. B. in einer
        // Testumgebung temporär abschalten (Zahlungen bleiben dann im Status Draft).
        // IOptionsMonitor statt IOptions, weil genau das der Fall ist, für den Azure App
        // Configuration ohne Neustart aktualisierte Werte liefern soll (siehe Kapitel 9).
        if (_featureFlags.CurrentValue.EnablePaymentApprovalWorkflow)
        {
            payment.SubmitForApproval();
        }

        // Der eigentliche SQL-Aufruf entsteht hier - die SqlClient-Instrumentierung greift
        // automatisch, ohne dass dieser Handler etwas davon wissen muss.
        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        stopwatch.Stop();
        Telemetry.PaymentSuccessCounter.Add(1);
        Telemetry.PaymentProcessingDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
        activity?.SetTag("payment.id", payment.Id.Value);
        activity?.SetStatus(ActivityStatusCode.Ok);

        return Result<CreatePaymentResultDto>.Success(new CreatePaymentResultDto(payment.Id, payment.Status));
    }
}
