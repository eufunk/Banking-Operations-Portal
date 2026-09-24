using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Banking.Api.Security;
using Banking.Application.Common.Pagination;
using Banking.Application.Transactions.Dtos;

namespace Banking.IntegrationTests;

/// <summary>Deckt den von Kapitel 17 geforderten Endpunkt GET /api/transactions ab.</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class TransactionsApiTests
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public TransactionsApiTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // API Response: die vollständige Pipeline (Routing -> Query-Binding -> Handler ->
    // echte SQL-Abfrage -> JSON) liefert die erwartete Form zurück, auch ohne Treffer.
    [Fact]
    public async Task GetAll_ForSeededAccountWithNoTransactions_ReturnsOkWithEmptyPage()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.IssueJwt(Roles.BankEmployee));

        var response = await client.GetAsync($"/api/transactions?accountId={_factory.SeededAccountId.Value}&page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionSummaryDto>>(IntegrationTestJsonOptions.Default);

        Assert.NotNull(result);
        Assert.Empty(result!.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetAll_ForUnknownAccount_Returns404()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.IssueJwt(Roles.BankEmployee));

        var response = await client.GetAsync($"/api/transactions?accountId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/transactions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
