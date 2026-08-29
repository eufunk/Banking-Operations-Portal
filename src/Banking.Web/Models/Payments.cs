namespace Banking.Web.Models;

public sealed record PaymentDetails(
    Guid Id,
    Guid SourceAccountId,
    string TargetAccountNumber,
    decimal Amount,
    string Currency,
    string Reference,
    PaymentStatus Status,
    DateTime CreatedAt);

public sealed record CreatePaymentResult(Guid Id, PaymentStatus Status);

/// <summary>Spiegelt Banking.Api.Contracts.Payments.CreatePaymentRequest - eigener Vertrag, siehe ADR-0002.</summary>
public sealed record CreatePaymentRequest(
    Guid SourceAccountId,
    string TargetAccountNumber,
    decimal Amount,
    string Currency,
    string Reference);
