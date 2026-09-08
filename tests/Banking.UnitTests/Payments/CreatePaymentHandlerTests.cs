using Banking.Application.Accounts;
using Banking.Application.Common;
using Banking.Application.Common.Options;
using Banking.Application.Customers;
using Banking.Application.Payments;
using Banking.Application.Payments.CreatePayment;
using Banking.Domain.Accounts;
using Banking.Domain.Common;
using Banking.Domain.Customers;
using Banking.Domain.Payments;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Banking.UnitTests.Payments;

/// <summary>
/// Testet CreatePaymentHandler isoliert: alle externen Abhängigkeiten (Repositories) sind
/// gemockt, der Validator ebenfalls (Standard: immer gültig) - so lässt sich jede
/// Verteidigungslinie des Handlers unabhängig von den anderen prüfen. Die
/// FluentValidation-Regeln selbst (Betrag &lt;= 0, Betrag über Limit, ...) haben eine eigene,
/// ungemockte Testklasse: <see cref="CreatePaymentValidatorTests"/> - siehe dort für die
/// Begründung dieser Aufteilung.
/// </summary>
public sealed class CreatePaymentHandlerTests
{
    private readonly IValidator<CreatePaymentCommand> _validator = Substitute.For<IValidator<CreatePaymentCommand>>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IOptionsMonitor<FeatureFlagsOptions> _featureFlags = Substitute.For<IOptionsMonitor<FeatureFlagsOptions>>();

    private readonly Customer _customer;
    private readonly Account _account;

    public CreatePaymentHandlerTests()
    {
        _validator.ValidateAsync(Arg.Any<CreatePaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _featureFlags.CurrentValue.Returns(new FeatureFlagsOptions { EnablePaymentApprovalWorkflow = false });

        _paymentRepository.ExistsSimilarRecentAsync(
                Arg.Any<AccountId>(), Arg.Any<AccountNumber>(), Arg.Any<Money>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _customer = Customer.Register(new CustomerNumber("C-1001"), "Erika", "Musterfrau", new Email("erika@example.com"));
        _account = Account.Open(new AccountNumber("DE89370400440532013000"), _customer.Id, AccountType.Checking, CurrencyCode.Eur);
        _account.Credit(new Money(1000m, CurrencyCode.Eur)); // Guthaben für Erfolgsfall/Kontostand-Test.

        _accountRepository.GetByIdAsync(_account.Id, Arg.Any<CancellationToken>()).Returns(_account);
        _customerRepository.GetByIdAsync(_customer.Id, Arg.Any<CancellationToken>()).Returns(_customer);
    }

    private CreatePaymentHandler CreateHandler() => new(
        _validator, _accountRepository, _customerRepository, _paymentRepository, _unitOfWork, _featureFlags);

    private CreatePaymentCommand ValidCommand(
        decimal amount = 100m, string currency = "EUR", string targetAccountNumber = "DE12500105170648489890")
        => new(_account.Id, targetAccountNumber, amount, currency, "Miete Januar");

    [Fact]
    public async Task Handle_WhenCommandInvalid_ReturnsValidationFailure()
    {
        _validator.ValidateAsync(Arg.Any<CreatePaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Amount", "Betrag muss größer als 0 sein.")]));

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.Invalid", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_WhenSourceAccountNotFound_ReturnsNotFound()
    {
        _accountRepository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns((Account?)null);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Account.NotFound", result.Error!.Code);
    }

    // "inactive source account" (Kapitel 16, Testfall-Liste). AccountStatus kennt kein
    // eigenes "Inactive" - Block() ist der Zustand, der ein Konto handlungsunfähig macht.
    [Fact]
    public async Task Handle_WhenSourceAccountBlocked_ReturnsConflict()
    {
        _account.Block();

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Account.NotActive", result.Error!.Code);
    }

    // "inactive customer" (Kapitel 16, Testfall-Liste) - neue Regel, siehe ADR-Notiz zu Kapitel 16.
    [Fact]
    public async Task Handle_WhenCustomerInactive_ReturnsConflict()
    {
        _customer.Deactivate();

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Customer.NotActive", result.Error!.Code);
    }

    // "invalid currency" (Kapitel 16, Testfall-Liste) - ein syntaktisch gültiger, aber von
    // der Anwendung nicht unterstützter Code.
    [Fact]
    public async Task Handle_WhenCurrencyNotSupported_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(ValidCommand(currency: "XXX"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.InvalidCurrency", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_WhenCurrencyDoesNotMatchAccountCurrency_ReturnsConflict()
    {
        var result = await CreateHandler().Handle(ValidCommand(currency: "USD"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.CurrencyMismatch", result.Error!.Code);
    }

    // "insufficient balance" (Kapitel 16, Testfall-Liste) - neue Regel, siehe ADR-Notiz zu Kapitel 16.
    [Fact]
    public async Task Handle_WhenBalanceInsufficient_ReturnsConflict()
    {
        var result = await CreateHandler().Handle(ValidCommand(amount: 1_000_000m), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.InsufficientFunds", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ForCreditAccount_AllowsPaymentBeyondBalance()
    {
        // Kreditkonten dürfen laut Domain-Modell (Account.Debit) ins Minus laufen - die
        // Kontostand-Prüfung im Handler klammert sie deshalb bewusst aus.
        var creditAccount = Account.Open(new AccountNumber("DE00500105170648480000"), _customer.Id, AccountType.Credit, CurrencyCode.Eur);
        _accountRepository.GetByIdAsync(creditAccount.Id, Arg.Any<CancellationToken>()).Returns(creditAccount);
        var command = new CreatePaymentCommand(creditAccount.Id, "DE12500105170648489890", 5_000m, "EUR", "Kreditrahmen nutzen");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // "invalid target account" (Kapitel 16, Testfall-Liste). Der Validator ist hier absichtlich
    // (per Standard-Setup) immer "gültig" gemockt - dieser Test prüft die Verteidigungslinie
    // im Handler selbst, unabhängig davon, ob eine vorgelagerte Validierung dieselbe
    // Eingabe schon abgefangen hätte (siehe Kommentar in CreatePaymentHandler).
    [Fact]
    public async Task Handle_WhenTargetAccountNumberInvalid_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(ValidCommand(targetAccountNumber: ""), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.InvalidTargetAccount", result.Error!.Code);
    }

    // "duplicate payment" (Kapitel 16, Testfall-Liste) - neue Regel, siehe ADR-Notiz zu Kapitel 16.
    [Fact]
    public async Task Handle_WhenSimilarPaymentWasJustSubmitted_ReturnsConflict()
    {
        _paymentRepository.ExistsSimilarRecentAsync(
                Arg.Any<AccountId>(), Arg.Any<AccountNumber>(), Arg.Any<Money>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.Duplicate", result.Error!.Code);
    }

    // "successful payment" (Kapitel 16, Testfall-Liste).
    [Fact]
    public async Task Handle_WhenAllChecksPass_ReturnsSuccessAndPersistsPayment()
    {
        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Draft, result.Value.Status);
        await _paymentRepository.Received(1).AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenApprovalWorkflowEnabled_SubmitsPaymentForApproval()
    {
        _featureFlags.CurrentValue.Returns(new FeatureFlagsOptions { EnablePaymentApprovalWorkflow = true });

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.PendingApproval, result.Value.Status);
    }
}
