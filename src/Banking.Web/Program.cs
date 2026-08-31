using System.Diagnostics;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Banking.Web.Components;
using Banking.Web.Security;
using Banking.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// --- Azure Configuration (Kapitel 9, siehe Banking.Api/Program.cs für die Begründung) ---
// Web hat mit Jwt:SigningKey ebenfalls ein Secret - dieselbe Vorbereitung wie in der Api,
// nur lokal (noch) ungenutzt, da keine Azure-Subscription vorhanden ist.
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

// --- Logging & Observability / OpenTelemetry (Kapitel 10) ---
// Web selbst hat keine eigene Geschäftslogik-Telemetrie (keine eigenen ActivitySource/
// Meter-Instrumente wie in Banking.Application) - der Zweck hier ist ausschließlich, die
// Trace-Id über den kompletten Weg Browser-Klick -> Web (BFF) -> Api -> SQL hinweg
// durchzureichen (W3C "traceparent"-Header, automatisch über die HttpClient-
// Instrumentierung). Ohne diesen Block würde jeder Api-Aufruf einen komplett neuen,
// unverbundenen Trace beginnen - man könnte einen Klick im Browser dann nicht mehr bis zur
// dahinterliegenden SQL-Abfrage zurückverfolgen.
const string WebServiceName = "Banking.Web";

builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId
        | ActivityTrackingOptions.SpanId
        | ActivityTrackingOptions.ParentId;
});

var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(WebServiceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();

        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            tracing.AddAzureMonitorTraceExporter(options => options.ConnectionString = appInsightsConnectionString);
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter((_, readerOptions) =>
                readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000);

        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            metrics.AddAzureMonitorMetricExporter(options => options.ConnectionString = appInsightsConnectionString);
        }
    });

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- Authentication/Authorization ---
// Cookie-Auth für den Browser<->Blazor-Server-Kanal. Die Api selbst kennt dieses Cookie
// nicht - sie validiert stattdessen unabhängig ein JWT, das AuthTokenHandler pro
// ausgehendem Aufruf aus genau diesen Cookie-Claims mintet (siehe Security/JwtTokenIssuer).
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<IJwtTokenIssuer, JwtTokenIssuer>();
builder.Services.AddScoped<AuthTokenHandler>();

var apiBaseUrl = builder.Configuration["BankingApi:BaseUrl"]
    ?? throw new InvalidOperationException("Konfiguration 'BankingApi:BaseUrl' fehlt.");

// Typisierte HttpClients statt eines geteilten HttpClient: jeder Ressourcen-Client
// bekommt seine eigene, unabhängig konfigurierbare Basis-Adresse/Policies (Timeouts,
// später z. B. Resilience-Handler pro Ressource), ADR-0002 - Web spricht ausschließlich
// über REST mit der Api, nie direkt mit der Application-/Infrastructure-Schicht.
// AuthTokenHandler haengt bei jedem Aufruf das aktuelle Nutzer-Token an (s.o.).
builder.Services.AddHttpClient<ICustomersApiClient, CustomersApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<IAccountsApiClient, AccountsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<ITransactionsApiClient, TransactionsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<IPaymentsApiClient, PaymentsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Logout als eigener, nicht-interaktiver POST-Endpunkt (kein Blazor-Circuit): SignOutAsync
// schreibt einen Set-Cookie-Header, das braucht direkten Response-Zugriff, keine bereits
// laufende SignalR-Verbindung. Antiforgery wird explizit geprüft, da ein einfacher
// MapPost-Endpunkt das nicht automatisch tut wie Blazors eigene EditForm-Pipeline.
app.MapPost("/account/logout", async (HttpContext http, IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(http);
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.Run();
