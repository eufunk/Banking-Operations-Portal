using Banking.Web.Components;
using Banking.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var apiBaseUrl = builder.Configuration["BankingApi:BaseUrl"]
    ?? throw new InvalidOperationException("Konfiguration 'BankingApi:BaseUrl' fehlt.");

// Typisierte HttpClients statt eines geteilten HttpClient: jeder Ressourcen-Client
// bekommt seine eigene, unabhängig konfigurierbare Basis-Adresse/Policies (Timeouts,
// später z. B. Resilience-Handler pro Ressource), ADR-0002 - Web spricht ausschließlich
// über REST mit der Api, nie direkt mit der Application-/Infrastructure-Schicht.
builder.Services.AddHttpClient<ICustomersApiClient, CustomersApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<IAccountsApiClient, AccountsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<ITransactionsApiClient, TransactionsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<IPaymentsApiClient, PaymentsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl));

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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
