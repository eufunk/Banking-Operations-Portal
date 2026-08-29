using Banking.Application.Accounts.Dtos;
using Banking.Application.Accounts.GetAccountDetails;
using Banking.Application.Common.Messaging;
using Banking.Application.Common.Pagination;
using Banking.Application.Customers.Dtos;
using Banking.Application.Customers.GetCustomerDetails;
using Banking.Application.Customers.SearchCustomers;
using Banking.Application.Payments.CreatePayment;
using Banking.Application.Payments.Dtos;
using Banking.Application.Payments.GetPayment;
using Banking.Application.Payments.GetPaymentStatus;
using Banking.Application.Transactions.Dtos;
using Banking.Application.Transactions.GetTransactionDetails;
using Banking.Application.Transactions.SearchTransactions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Query Handler
        services.AddScoped<IQueryHandler<SearchCustomersQuery, PagedResult<CustomerSummaryDto>>, SearchCustomersHandler>();
        services.AddScoped<IQueryHandler<GetCustomerDetailsQuery, CustomerDetailsDto>, GetCustomerDetailsHandler>();
        services.AddScoped<IQueryHandler<GetAccountDetailsQuery, AccountDetailsDto>, GetAccountDetailsHandler>();
        services.AddScoped<IQueryHandler<SearchTransactionsQuery, PagedResult<TransactionSummaryDto>>, SearchTransactionsHandler>();
        services.AddScoped<IQueryHandler<GetTransactionDetailsQuery, TransactionDetailsDto>, GetTransactionDetailsHandler>();
        services.AddScoped<IQueryHandler<GetPaymentQuery, PaymentDetailsDto>, GetPaymentHandler>();
        services.AddScoped<IQueryHandler<GetPaymentStatusQuery, PaymentStatusDto>, GetPaymentStatusHandler>();

        // Command Handler
        services.AddScoped<ICommandHandler<CreatePaymentCommand, CreatePaymentResultDto>, CreatePaymentHandler>();

        // Validators
        services.AddScoped<IValidator<SearchCustomersQuery>, SearchCustomersValidator>();
        services.AddScoped<IValidator<SearchTransactionsQuery>, SearchTransactionsValidator>();
        services.AddScoped<IValidator<CreatePaymentCommand>, CreatePaymentValidator>();

        return services;
    }
}
