using Banking.Application.Common.Messaging;
using Banking.Application.Customers.Dtos;
using Banking.Domain.Customers;

namespace Banking.Application.Customers.GetCustomerSapProfile;

public sealed record GetCustomerSapProfileQuery(CustomerId CustomerId) : IQuery<CustomerSapProfileDto>;
