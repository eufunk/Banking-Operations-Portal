using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Banking.Domain.Accounts;
using Banking.Domain.Common;
using Banking.Domain.Customers;
using Banking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Banking.IntegrationTests;

/// <summary>
/// Startet die echte Api In-Process (ASP.NET Core <c>TestServer</c>) statt über das
/// Netzwerk: echte Dependency Injection, echte Middleware-Pipeline (Routing,
/// Authentication/Authorization, ProblemDetails-Fehlerbehandlung), echte
/// Application-Handler, echte Datenbank - nichts davon wird gemockt (siehe Kapitel-17-
/// Vorgabe "Vermeide Mocking der kompletten Application Layer").
///
/// Datenbank: LocalDB statt Testcontainers, weil auf dieser Maschine kein Docker
/// installiert ist ("Verwende nach Möglichkeit Testcontainers" - hier nicht möglich,
/// siehe docs/architecture/testing.md und ADR #22). Jede Instanz dieser Factory
/// bekommt eine frisch angelegte, eindeutig benannte Datenbank (per Migration erzeugt,
/// in <see cref="DisposeAsync"/> wieder gelöscht) - echte SQL-Server-Engine, aber ohne
/// die Container-Isolation, die Testcontainers bieten würde.
/// </summary>
public sealed class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtSigningKey = "integration-test-signing-key-not-for-production-0123456789";
    public const string JwtIssuer = "BankingOperationsPortal.Tests";
    public const string JwtAudience = "BankingOperationsPortal.Api.Tests";

    private readonly string _databaseName = $"BankingOperationsPortal_IntegrationTests_{Guid.NewGuid():N}";

    public CustomerId SeededCustomerId { get; private set; }
    public AccountId SeededAccountId { get; private set; }
    public AccountNumber SeededAccountNumber { get; private set; }

    public IntegrationTestWebApplicationFactory()
    {
        // Program.cs liest ConnectionStrings:BankingDatabase (in AddInfrastructure) und
        // Jwt:SigningKey/Issuer/Audience eager aus builder.Configuration, BEVOR
        // builder.Build() läuft. WebApplicationFactory.ConfigureWebHost/
        // ConfigureAppConfiguration greift erst am Build()-Interception-Punkt - für diese
        // eager gelesenen Werte damit zu spät. Umgebungsvariablen dagegen werden bereits
        // von WebApplicationBuilder.CreateBuilder() selbst eingelesen, wie sie z. B. schon
        // sonst überall in diesem Projekt lokal per PowerShell gesetzt werden - deshalb hier
        // derselbe Mechanismus statt ConfigureAppConfiguration.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__BankingDatabase",
            $"Server={ResolveLocalDbServer()};Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", JwtSigningKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", JwtAudience);
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        await dbContext.Database.MigrateAsync();

        // Ein Referenz-Kunde/-Konto, das alle Tests lesend nutzen können, ohne sich
        // gegenseitig Daten wegzunehmen - schreibende Tests (z. B. POST /api/payments)
        // erzeugen zusätzlich jeweils eigene, eindeutige Datensätze.
        var customer = Customer.Register(new CustomerNumber("IT-0001"), "Integration", "Test", new Email("integration.test@example.com"));
        var account = Account.Open(new AccountNumber("DE00500105170648480001"), customer.Id, AccountType.Checking, CurrencyCode.Eur);
        account.Credit(new Money(10_000m, CurrencyCode.Eur));

        dbContext.Customers.Add(customer);
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();

        SeededCustomerId = customer.Id;
        SeededAccountId = account.Id;
        SeededAccountNumber = account.AccountNumber;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
        }

        await base.DisposeAsync();
    }

    /// <summary>
    /// Der Alias "(localdb)\MSSQLLocalDB" schlägt auf dieser Maschine wiederkehrend mit
    /// einem SNI-/SQLUserInstance.dll-Fehler fehl (bekannter LocalDB-Quirk, siehe
    /// docs/architecture/testing.md) - die tatsächliche, aktuell aktive Named Pipe über
    /// "sqllocaldb info" funktioniert dagegen zuverlässig. Fällt auf den Alias zurück,
    /// falls "sqllocaldb" nicht verfügbar ist (z. B. auf einer anderen Maschine/CI).
    /// </summary>
    private static string ResolveLocalDbServer()
    {
        const string fallback = @"(localdb)\MSSQLLocalDB";

        try
        {
            using var process = Process.Start(new ProcessStartInfo("sqllocaldb", "info MSSQLLocalDB")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            });

            if (process is null)
            {
                return fallback;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            var match = Regex.Match(output, @"pipe\\[^\s]+", RegexOptions.IgnoreCase);
            return match.Success ? $@"np:\\.\{match.Value}" : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    /// <summary>
    /// Baut ein echtes, gültig signiertes JWT mit denselben Mitteln, die auch
    /// Banking.Web/Security/JwtTokenIssuer verwendet - die Api validiert es unabhängig
    /// und unterscheidet nicht, ob das Token von Banking.Web oder von hier stammt.
    /// </summary>
    public string IssueJwt(params string[] roles)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, "integration-test-user") };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
