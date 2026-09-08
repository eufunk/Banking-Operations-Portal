using Banking.Application.Common.Options;
using Banking.Application.Payments.CreatePayment;
using Banking.Domain.Accounts;
using Microsoft.Extensions.Options;

namespace Banking.UnitTests.Payments;

public sealed class CreatePaymentValidatorTests
{
    private static CreatePaymentValidator CreateValidator(decimal maxAmountPerPayment = 50_000m)
        => new(Options.Create(new PaymentLimitsOptions { MaxAmountPerPayment = maxAmountPerPayment }));

    private static CreatePaymentCommand ValidCommand(decimal amount = 100m) => new(
        AccountId.New(),
        "DE89370400440532013000",
        amount,
        "EUR",
        "Miete Januar");

    // "Payment amount <= 0" (Kapitel 16, Testfall-Liste).
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithAmountLessOrEqualZero_IsInvalid(decimal amount)
    {
        var validator = CreateValidator();
        var command = ValidCommand(amount);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    // "Payment amount > allowed limit" (Kapitel 16, Testfall-Liste).
    [Fact]
    public void Validate_WithAmountAboveConfiguredLimit_IsInvalid()
    {
        var validator = CreateValidator(maxAmountPerPayment: 1_000m);
        var command = ValidCommand(amount: 1_000.01m);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithAmountAtConfiguredLimit_IsValid()
    {
        var validator = CreateValidator(maxAmountPerPayment: 1_000m);
        var command = ValidCommand(amount: 1_000m);

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyTargetAccountNumber_IsInvalid()
    {
        var validator = CreateValidator();
        var command = ValidCommand() with { TargetAccountNumber = "" };

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyReference_IsInvalid()
    {
        var validator = CreateValidator();
        var command = ValidCommand() with { Reference = "" };

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var validator = CreateValidator();
        var command = ValidCommand();

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
