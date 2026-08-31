using Banking.Application.Accounts;
using Banking.Application.Common;
using Banking.Application.Common.Options;
using Banking.Application.Customers;
using Banking.Application.Imports;
using Banking.Application.Payments;
using Banking.Application.Sap;
using Banking.Application.Transactions;
using Banking.Infrastructure.Persistence;
using Banking.Infrastructure.Persistence.Repositories;
using Banking.Infrastructure.Sap;
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
        services.AddScoped<IImportJobRepository, ImportJobRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // --- SAP-Integration (Kapitel 11) ---
        // Anti-Corruption Layer (ADR #9): Domain/Application kennen nur ISapCustomerService,
        // nie SAP-spezifische Typen. Ohne konfigurierte Sap:BaseUrl (Standardfall - kein
        // SAP-System verfügbar) läuft MockSapCustomerService; ist eine BaseUrl konfiguriert,
        // wird stattdessen die echte, HttpClient-basierte Anbindung mit
        // Timeout/Retry/Circuit-Breaker registriert. Details/Begründung: ADR #19,
        // docs/architecture/sap-integration.md.
        var sapOptions = configuration.GetSection(SapOptions.SectionName).Get<SapOptions>() ?? new SapOptions();

        if (sapOptions.BaseUrl is not null)
        {
            services.AddHttpClient<ISapCustomerService, SapRfcCustomerService>(client =>
                {
                    client.BaseAddress = sapOptions.BaseUrl;
                })
                .AddStandardResilienceHandler(resilience =>
                {
                    resilience.Retry.MaxRetryAttempts = sapOptions.MaxRetryAttempts;
                    resilience.AttemptTimeout.Timeout = sapOptions.RequestTimeout;
                    resilience.TotalRequestTimeout.Timeout = sapOptions.RequestTimeout * (sapOptions.MaxRetryAttempts + 1);
                    // CircuitBreaker.SamplingDuration muss mindestens die doppelte
                    // AttemptTimeout betragen (Validierung des Resilience-Pakets) - explizit
                    // gesetzt, damit das auch bei einer künftig größer konfigurierten
                    // RequestTimeout zuverlässig gilt.
                    resilience.CircuitBreaker.SamplingDuration = resilience.AttemptTimeout.Timeout * 2;
                });
        }
        else
        {
            services.AddScoped<ISapCustomerService, MockSapCustomerService>();
        }

        return services;
    }
}
