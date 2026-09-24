using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Banking.Api.Contracts.Payments;
using Banking.Api.Security;
using Banking.Application.Payments.Dtos;

namespace Banking.IntegrationTests;

/// <summary>
/// Deckt die von Kapitel 17 explizit geforderten Endpunkte POST /api/payments und
/// GET /api/payments/{id} ab, jeweils entlang der geforderten Dimensionen: HTTP Status
/// Codes, Validation, Persistence, API Response, Error Handling.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class PaymentsApiTests
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public PaymentsApiTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthenticatedClient(params string[] roles)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.IssueJwt(roles));
        return client;
    }

    private CreatePaymentRequest ValidRequest(decimal amount = 25m, string reference = "Integrationstest") => new(
        _factory.SeededAccountId.Value,
        "DE12500105170648489890",
        amount,
        "EUR",
        reference);

    // HTTP Status Codes + Persistence + API Response.
    [Fact]
    public async Task Create_WithValidRequest_Returns201AndPersistsPayment()
    {
        var client = CreateAuthenticatedClient(Roles.BankEmployee);

        var response = await client.PostAsJsonAsync(
            "/api/payments", ValidRequest(reference: nameof(Create_WithValidRequest_Returns201AndPersistsPayment)), IntegrationTestJsonOptions.Default);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<CreatePaymentResultDto>(IntegrationTestJsonOptions.Default);
        Assert.NotNull(created);

        // Persistence: nicht nur die Antwort prüfen, sondern über einen zweiten, echten
        // Request nachweisen, dass der Zahlungsauftrag tatsächlich in der Datenbank liegt.
        var getResponse = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var persisted = await getResponse.Content.ReadFromJsonAsync<PaymentDetailsDto>(IntegrationTestJsonOptions.Default);
        Assert.NotNull(persisted);
        Assert.Equal(created!.Id.Value, persisted!.Id.Value);
        Assert.Equal(25m, persisted.Amount);
    }

    // Validation + Error Handling.
    [Fact]
    public async Task Create_WithZeroAmount_Returns400WithProblemDetails()
    {
        var client = CreateAuthenticatedClient(Roles.BankEmployee);

        var response = await client.PostAsJsonAsync("/api/payments", ValidRequest(amount: 0m), IntegrationTestJsonOptions.Default);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(IntegrationTestJsonOptions.Default);
        Assert.Equal("Payment.Invalid", problem!.Code);
        Assert.NotNull(problem.Errors);
        Assert.NotEmpty(problem.Errors!);
    }

    // Error Handling (fachlicher Konflikt, kein technischer Fehler) + HTTP Status Codes.
    [Fact]
    public async Task Create_ForUnknownSourceAccount_Returns404()
    {
        var client = CreateAuthenticatedClient(Roles.BankEmployee);
        var request = new CreatePaymentRequest(Guid.NewGuid(), "DE12500105170648489890", 10m, "EUR", "Unbekanntes Konto");

        var response = await client.PostAsJsonAsync("/api/payments", request, IntegrationTestJsonOptions.Default);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(IntegrationTestJsonOptions.Default);
        Assert.Equal("Account.NotFound", problem!.Code);
    }

    // HTTP Status Codes: die komplette Middleware-Pipeline (inkl. Authentication) läuft
    // echt mit - kein gemockter Auth-Handler.
    [Fact]
    public async Task Create_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/payments", ValidRequest(), IntegrationTestJsonOptions.Default);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForNonExistentPayment_Returns404()
    {
        var client = CreateAuthenticatedClient(Roles.BankEmployee);

        var response = await client.GetAsync($"/api/payments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
