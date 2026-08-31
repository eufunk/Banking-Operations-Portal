using Banking.Application.Accounts.Dtos;
using Banking.Application.Accounts.GetAccountDetails;
using Banking.Application.Accounts.SearchAccounts;
using Banking.Application.Common.Messaging;
using Banking.Application.Common.Options;
using Banking.Application.Common.Pagination;
using Banking.Application.Customers.Dtos;
using Banking.Application.Customers.GetCustomerDetails;
using Banking.Application.Customers.GetCustomerSapProfile;
using Banking.Application.Customers.SearchCustomers;
using Banking.Application.Payments.ApprovePayment;
using Banking.Application.Payments.CreatePayment;
using Banking.Application.Payments.Dtos;
using Banking.Application.Payments.GetPayment;
using Banking.Application.Payments.GetPaymentStatus;
using Banking.Application.Payments.RejectPayment;
using Banking.Application.Payments.SearchPayments;
using Banking.Application.Transactions.Dtos;
using Banking.Application.Transactions.GetTransactionDetails;
using Banking.Application.Transactions.SearchTransactions;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration (kein Secret) - siehe docs/architecture/configuration.md für die
        // vollständige Konfigurationshierarchie und die Trennung Secrets/Configuration.
        services.Configure<PaymentLimitsOptions>(configuration.GetSection(PaymentLimitsOptions.SectionName));
        services.Configure<TimeoutOptions>(configuration.GetSection(TimeoutOptions.SectionName));
        services.Configure<FeatureFlagsOptions>(configuration.GetSection(FeatureFlagsOptions.SectionName));
        services.Configure<SapOptions>(configuration.GetSection(SapOptions.SectionName));

        // Query Handler
        services.AddScoped<IQueryHandler<SearchCustomersQuery, PagedResult<CustomerSummaryDto>>, SearchCustomersHandler>();
        services.AddScoped<IQueryHandler<GetCustomerDetailsQuery, CustomerDetailsDto>, GetCustomerDetailsHandler>();
        services.AddScoped<IQueryHandler<GetCustomerSapProfileQuery, CustomerSapProfileDto>, GetCustomerSapProfileHandler>();
        services.AddScoped<IQueryHandler<GetAccountDetailsQuery, AccountDetailsDto>, GetAccountDetailsHandler>();
        services.AddScoped<IQueryHandler<SearchAccountsQuery, PagedResult<AccountDetailsDto>>, SearchAccountsHandler>();
        services.AddScoped<IQueryHandler<SearchTransactionsQuery, PagedResult<TransactionSummaryDto>>, SearchTransactionsHandler>();
        services.AddScoped<IQueryHandler<GetTransactionDetailsQuery, TransactionDetailsDto>, GetTransactionDetailsHandler>();
        services.AddScoped<IQueryHandler<GetPaymentQuery, PaymentDetailsDto>, GetPaymentHandler>();
        services.AddScoped<IQueryHandler<GetPaymentStatusQuery, PaymentStatusDto>, GetPaymentStatusHandler>();
        services.AddScoped<IQueryHandler<SearchPaymentsQuery, PagedResult<PaymentDetailsDto>>, SearchPaymentsHandler>();

        // Command Handler
        services.AddScoped<ICommandHandler<CreatePaymentCommand, CreatePaymentResultDto>, CreatePaymentHandler>();
        services.AddScoped<ICommandHandler<ApprovePaymentCommand, PaymentStatusDto>, ApprovePaymentHandler>();
        services.AddScoped<ICommandHandler<RejectPaymentCommand, PaymentStatusDto>, RejectPaymentHandler>();

        // Validators
        services.AddScoped<IValidator<SearchCustomersQuery>, SearchCustomersValidator>();
        services.AddScoped<IValidator<SearchAccountsQuery>, SearchAccountsValidator>();
        services.AddScoped<IValidator<SearchTransactionsQuery>, SearchTransactionsValidator>();
        services.AddScoped<IValidator<CreatePaymentCommand>, CreatePaymentValidator>();
        services.AddScoped<IValidator<SearchPaymentsQuery>, SearchPaymentsValidator>();

        return services;
    }
}
