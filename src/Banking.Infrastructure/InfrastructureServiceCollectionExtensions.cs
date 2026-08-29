using Banking.Infrastructure.Persistence;
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

        services.AddDbContext<BankingDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(BankingDbContext).Assembly.FullName)));

        return services;
    }
}
