using Banking.Application.Common;
using Banking.Application.Common.Errors;
using Banking.Application.Common.Messaging;
using Banking.Application.Customers.Dtos;

namespace Banking.Application.Customers.GetCustomerDetails;

public sealed class GetCustomerDetailsHandler : IQueryHandler<GetCustomerDetailsQuery, CustomerDetailsDto>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerDetailsHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Result<CustomerDetailsDto>> Handle(GetCustomerDetailsQuery query, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(query.CustomerId, cancellationToken);
        if (customer is null)
        {
            return Result<CustomerDetailsDto>.Failure(Error.NotFound(
                "Customer.NotFound", $"Kunde {query.CustomerId} wurde nicht gefunden."));
        }

        var dto = new CustomerDetailsDto(
            customer.Id,
            customer.CustomerNumber.Value,
            customer.FirstName,
            customer.LastName,
            customer.Email.Value,
            customer.Status,
            customer.CreatedAt);

        return Result<CustomerDetailsDto>.Success(dto);
    }
}
