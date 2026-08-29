using FluentValidation;

namespace Banking.Application.Customers.SearchCustomers;

public sealed class SearchCustomersValidator : AbstractValidator<SearchCustomersQuery>
{
    public SearchCustomersValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1)
            .WithMessage("Page muss mindestens 1 sein.");

        RuleFor(q => q.PageSize).InclusiveBetween(1, 100)
            .WithMessage("PageSize muss zwischen 1 und 100 liegen.");

        RuleFor(q => q.SearchTerm).MaximumLength(200)
            .WithMessage("Suchbegriff darf maximal 200 Zeichen lang sein.");
    }
}
