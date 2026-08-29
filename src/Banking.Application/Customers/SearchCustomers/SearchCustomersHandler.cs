using Banking.Application.Common;
using Banking.Application.Common.Errors;
using Banking.Application.Common.Messaging;
using Banking.Application.Common.Pagination;
using Banking.Application.Customers.Dtos;
using FluentValidation;

namespace Banking.Application.Customers.SearchCustomers;

public sealed class SearchCustomersHandler : IQueryHandler<SearchCustomersQuery, PagedResult<CustomerSummaryDto>>
{
    private readonly IValidator<SearchCustomersQuery> _validator;
    private readonly ICustomerRepository _customerRepository;

    public SearchCustomersHandler(IValidator<SearchCustomersQuery> validator, ICustomerRepository customerRepository)
    {
        _validator = validator;
        _customerRepository = customerRepository;
    }

    public async Task<Result<PagedResult<CustomerSummaryDto>>> Handle(SearchCustomersQuery query, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<PagedResult<CustomerSummaryDto>>.Failure(Error.Validation(
                "Customers.Search.Invalid",
                "Die Suchparameter sind ungültig.",
                validationResult.Errors.Select(e => e.ErrorMessage).ToArray()));
        }

        var customers = await _customerRepository.SearchAsync(
            query.SearchTerm, query.Status, query.Page, query.PageSize, cancellationToken);

        var items = customers.Items
            .Select(c => new CustomerSummaryDto(c.Id, c.CustomerNumber.Value, $"{c.FirstName} {c.LastName}", c.Email.Value, c.Status))
            .ToList();

        return Result<PagedResult<CustomerSummaryDto>>.Success(
            new PagedResult<CustomerSummaryDto>(items, customers.TotalCount, customers.Page, customers.PageSize));
    }
}
