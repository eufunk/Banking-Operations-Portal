using System.Text;
using System.Text.Json.Serialization;
using Azure.Identity;
using Banking.Api.Middleware;
using Banking.Api.Serialization;
using Banking.Application;
using Banking.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- Azure Configuration (Kapitel 9) ---
// Nur aktiv, wenn eine echte Azure-Ressource konfiguriert ist - lokal ohne Azure-
// Subscription bleibt dieser Block wirkungslos (appConfigEndpoint/keyVaultUri sind dann
// leer). DefaultAzureCredential nutzt beim Deployment in Azure automatisch die Managed
// Identity des App Service - kein Secret im Code oder in appsettings nötig. Muss VOR
// AddInfrastructure() laufen, damit z. B. eine per Key Vault überschriebene Connection
// String rechtzeitig verfügbar ist. Details: docs/architecture/configuration.md.
var appConfigEndpoint = builder.Configuration["AzureAppConfiguration:Endpoint"];
if (!string.IsNullOrWhiteSpace(appConfigEndpoint))
{
    var credential = new DefaultAzureCredential();

    builder.Configuration.AddAzureAppConfiguration(options =>
        options.Connect(new Uri(appConfigEndpoint), credential)
            .ConfigureKeyVault(kv => kv.SetCredential(credential))
            .UseFeatureFlags());
}

var keyVaultUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

// Add services to the container.

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new StronglyTypedIdJsonConverterFactory());
        // Lesbare Enum-Werte ("Active" statt 0) - konsistent mit der Speicherung als string in der DB.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ProblemDetails (RFC 7807) für alle Fehlerantworten - auch für Fälle, die [ApiController]
// selbst erzeugt (z. B. ungültiges Model Binding), nicht nur für unsere eigenen.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// --- Authentication/Authorization ---
// JWT-Bearer, ausgestellt von Banking.Web nach erfolgreichem Cookie-Login (BFF-Pattern:
// Web authentifiziert den Browser-Nutzer per Cookie, mintet pro Api-Aufruf ein
// kurzlebiges JWT mit den Rollen-Claims des Nutzers). Die Api validiert dieses Token
// komplett unabhängig von der UI - das ist die eigentliche Sicherheitsgrenze, nicht die
// Blazor-Seite. Symmetrischer Schlüssel nur für dieses lokale/Demo-Setup; in Produktion
// würde hier stattdessen gegen die öffentlichen Signaturschlüssel von Azure AD/Entra ID
// validiert (OIDC-Discovery), ohne einen geteilten Secret-Schlüssel zu benötigen.
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey ist nicht konfiguriert (siehe User Secrets).");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer ist nicht konfiguriert.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience ist nicht konfiguriert.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true,
            // Tokens sind absichtlich kurzlebig (siehe Web/Security/JwtTokenIssuer) -
            // keine zusätzliche Toleranz für abgelaufene Tokens nötig.
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

// Secure by default: jeder Endpunkt verlangt einen authentifizierten Nutzer, sofern er
// nicht explizit [AllowAnonymous] markiert ist. Verhindert den klassischen Fehler "neuer
// Controller vergisst [Authorize]" - hier müsste man Sicherheit aktiv ABschalten, nicht anschalten.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Globaler Exception Handler muss so früh wie möglich in der Pipeline stehen, damit er
// Exceptions aus allen nachfolgenden Middlewares/Controllern abfangen kann.
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Reine Schema-Dokumentation, kein sensibler Inhalt - AllowAnonymous trotz
    // "secure by default"-Fallback-Policy, sonst bräuchte man zum Browsen der Doku
    // bereits ein gültiges Token.
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
