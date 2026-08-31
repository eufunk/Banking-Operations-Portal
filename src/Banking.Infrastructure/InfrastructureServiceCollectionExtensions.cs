using Banking.Application.Accounts;
using Banking.Application.Common;
using Banking.Application.Common.Options;
using Banking.Application.Customers;
using Banking.Application.Payments;
using Banking.Application.Transactions;
using Banking.Infrastructure.Persistence;
using Banking.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BankingDatabase")
            ?? throw new InvalidOperationException(
                "Connection String 'BankingDatabase' ist nicht konfiguriert (appsettings/User Secrets).");

        // Configuration (kein Secret) - konfigurierbares Command-Timeout statt Magic Number.
        var timeouts = configuration.GetSection(TimeoutOptions.SectionName).Get<TimeoutOptions>() ?? new TimeoutOptions();

        services.AddDbContext<BankingDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(BankingDbContext).Assembly.FullName);
                sql.CommandTimeout((int)timeouts.DatabaseCommandTimeout.TotalSeconds);
            }));

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
