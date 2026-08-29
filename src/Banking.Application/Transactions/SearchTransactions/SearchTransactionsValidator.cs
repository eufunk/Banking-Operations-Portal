using FluentValidation;

namespace Banking.Application.Transactions.SearchTransactions;

public sealed class SearchTransactionsValidator : AbstractValidator<SearchTransactionsQuery>
{
    public SearchTransactionsValidator()
    {
        RuleFor(q => q.AccountId.Value).NotEqual(Guid.Empty)
            .WithMessage("AccountId ist erforderlich.");

        RuleFor(q => q.Page).GreaterThanOrEqualTo(1)
            .WithMessage("Page muss mindestens 1 sein.");

        RuleFor(q => q.PageSize).InclusiveBetween(1, 100)
            .WithMessage("PageSize muss zwischen 1 und 100 liegen.");

        RuleFor(q => q)
            .Must(q => q.From is null || q.To is null || q.From <= q.To)
            .WithMessage("'From' darf nicht nach 'To' liegen.");
    }
}
