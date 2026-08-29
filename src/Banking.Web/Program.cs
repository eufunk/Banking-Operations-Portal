using Banking.Web.Components;
using Banking.Web.Security;
using Banking.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

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
