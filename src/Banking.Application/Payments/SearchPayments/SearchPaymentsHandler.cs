using Banking.Application.Common;
using Banking.Application.Common.Errors;
using Banking.Application.Common.Messaging;
using Banking.Application.Common.Pagination;
using Banking.Application.Payments.Dtos;
using FluentValidation;

namespace Banking.Application.Payments.SearchPayments;

public sealed class SearchPaymentsHandler : IQueryHandler<SearchPaymentsQuery, PagedResult<PaymentDetailsDto>>
{
    private readonly IValidator<SearchPaymentsQuery> _validator;
    private readonly IPaymentRepository _paymentRepository;

    public SearchPaymentsHandler(IValidator<SearchPaymentsQuery> validator, IPaymentRepository paymentRepository)
    {
        _validator = validator;
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<PagedResult<PaymentDetailsDto>>> Handle(SearchPaymentsQuery query, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<PagedResult<PaymentDetailsDto>>.Failure(Error.Validation(
                "Payments.Search.Invalid",
                "Die Suchparameter sind ungültig.",
                validationResult.Errors.Select(e => e.ErrorMessage).ToArray()));
        }

        var payments = await _paymentRepository.SearchAsync(
            query.SourceAccountId, query.Status, query.Page, query.PageSize, cancellationToken);

        var items = payments.Items
            .Select(p => new PaymentDetailsDto(
                p.Id, p.SourceAccountId, p.TargetAccountNumber.Value, p.Amount.Amount, p.Amount.Currency.Value, p.Reference.Value, p.Status, p.CreatedAt))
            .ToList();

        return Result<PagedResult<PaymentDetailsDto>>.Success(
            new PagedResult<PaymentDetailsDto>(items, payments.TotalCount, payments.Page, payments.PageSize));
    }
}
