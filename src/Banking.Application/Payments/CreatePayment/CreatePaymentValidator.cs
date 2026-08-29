using Banking.Domain.Accounts;
using Banking.Domain.Payments;
using FluentValidation;

namespace Banking.Application.Payments.CreatePayment;

/// <summary>
/// Prüft nur die Eingabeform (leer, zu lang, Betrag &lt;= 0) - sammelt dabei alle
/// Fehler auf einmal, gut für Formulare/API-Clients. Die eigentlichen Geschäftsregeln
/// (Konto aktiv, Währung passt) prüft der Handler, weil sie einen DB-Zugriff brauchen.
/// Value Objects im Domain-Modell bleiben zusätzlich die letzte Verteidigungslinie,
/// unabhängig davon, ob ein Aufrufer diesen Validator überhaupt durchläuft.
/// </summary>
public sealed class CreatePaymentValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentValidator()
    {
        RuleFor(c => c.SourceAccountId.Value).NotEqual(Guid.Empty)
            .WithMessage("Quellkonto ist erforderlich.");

        RuleFor(c => c.TargetAccountNumber).NotEmpty().MaximumLength(AccountNumber.MaxLength)
            .WithMessage($"Zielkontonummer ist erforderlich und darf maximal {AccountNumber.MaxLength} Zeichen lang sein.");

        RuleFor(c => c.Amount).GreaterThan(0m)
            .WithMessage("Der Betrag muss größer als 0 sein.");

        RuleFor(c => c.Currency).NotEmpty().Length(3)
            .WithMessage("Währung muss ein 3-stelliger ISO-4217-Code sein.");

        RuleFor(c => c.Reference).NotEmpty().MaximumLength(PaymentReference.MaxLength)
            .WithMessage($"Verwendungszweck ist erforderlich und darf maximal {PaymentReference.MaxLength} Zeichen lang sein.");
    }
}
