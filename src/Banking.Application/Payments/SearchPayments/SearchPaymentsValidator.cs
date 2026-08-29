using FluentValidation;

namespace Banking.Application.Payments.SearchPayments;

public sealed class SearchPaymentsValidator : AbstractValidator<SearchPaymentsQuery>
{
    public SearchPaymentsValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1)
            .WithMessage("Page muss mindestens 1 sein.");

        RuleFor(q => q.PageSize).InclusiveBetween(1, 100)
            .WithMessage("PageSize muss zwischen 1 und 100 liegen.");
    }
}
