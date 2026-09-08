using Banking.Domain.Accounts;
using Banking.Domain.Common;
using Banking.Domain.Common.Exceptions;
using Banking.Domain.Payments;

namespace Banking.UnitTests.Domain.Payments;

public sealed class PaymentTests
{
    private static Payment CreateValidPayment() => Payment.Create(
        AccountId.New(),
        new AccountNumber("DE89370400440532013000"),
        new Money(100m, CurrencyCode.Eur),
        new PaymentReference("Test-Zahlung"));

    [Fact]
    public void Create_WithZeroAmount_Throws()
    {
        var act = () => Payment.Create(
            AccountId.New(),
            new AccountNumber("DE89370400440532013000"),
            new Money(0m, CurrencyCode.Eur),
            new PaymentReference("Test"));

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithNegativeAmount_Throws()
    {
        var act = () => Payment.Create(
            AccountId.New(),
            new AccountNumber("DE89370400440532013000"),
            new Money(-10m, CurrencyCode.Eur),
            new PaymentReference("Test"));

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_ReturnsPaymentInDraftStatus()
    {
        var payment = CreateValidPayment();

        Assert.Equal(PaymentStatus.Draft, payment.Status);
    }

    [Fact]
    public void SubmitForApproval_FromDraft_TransitionsToPendingApproval()
    {
        var payment = CreateValidPayment();

        payment.SubmitForApproval();

        Assert.Equal(PaymentStatus.PendingApproval, payment.Status);
    }

    // "invalid payment status transition" (Kapitel 16, Testfall-Liste): Approve() ist nur
    // aus PendingApproval erlaubt - ein Draft-Zahlungsauftrag wurde nie zur Freigabe eingereicht.
    [Fact]
    public void Approve_FromDraft_ThrowsInvalidStatusTransition()
    {
        var payment = CreateValidPayment();

        var act = payment.Approve;

        Assert.Throws<InvalidStatusTransitionException>(act);
    }

    [Fact]
    public void Approve_FromPendingApproval_TransitionsToApproved()
    {
        var payment = CreateValidPayment();
        payment.SubmitForApproval();

        payment.Approve();

        Assert.Equal(PaymentStatus.Approved, payment.Status);
    }

    [Fact]
    public void Reject_FromApproved_ThrowsInvalidStatusTransition()
    {
        // Reject ist laut Domain-Modell nur aus PendingApproval oder Approved erlaubt -
        // dieser Test zielt bewusst auf einen Status, der das NICHT ist (Draft).
        var payment = CreateValidPayment();

        var act = payment.Reject;

        Assert.Throws<InvalidStatusTransitionException>(act);
    }

    [Fact]
    public void MarkExecuted_FromApproved_WithoutSubmittedToNetwork_ThrowsInvalidStatusTransition()
    {
        var payment = CreateValidPayment();
        payment.SubmitForApproval();
        payment.Approve();

        // MarkExecuted verlangt Submitted, nicht Approved - der Zwischenschritt
        // MarkSubmittedToNetwork() wurde hier absichtlich übersprungen.
        var act = payment.MarkExecuted;

        Assert.Throws<InvalidStatusTransitionException>(act);
    }

    [Fact]
    public void FullLifecycle_DraftToExecuted_Succeeds()
    {
        var payment = CreateValidPayment();

        payment.SubmitForApproval();
        payment.Approve();
        payment.MarkSubmittedToNetwork();
        payment.MarkExecuted();

        Assert.Equal(PaymentStatus.Executed, payment.Status);
    }
}
