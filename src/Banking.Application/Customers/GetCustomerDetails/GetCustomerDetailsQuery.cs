using Banking.Application.Common.Messaging;
using Banking.Application.Customers.Dtos;
using Banking.Domain.Customers;

namespace Banking.Application.Customers.GetCustomerDetails;

public sealed record GetCustomerDetailsQuery(CustomerId CustomerId) : IQuery<CustomerDetailsDto>;
