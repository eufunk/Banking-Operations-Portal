using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Banking.Api.Security;
using Banking.Application.Common.Pagination;
using Banking.Application.Customers.Dtos;

namespace Banking.IntegrationTests;

/// <summary>Deckt den von Kapitel 17 geforderten Endpunkt GET /api/customers ab.</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class CustomersApiTests
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public CustomersApiTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // API Response + Persistence: der in der Factory angelegte Referenzkunde muss über
    // eine echte SQL-Abfrage wieder auffindbar sein.
    [Fact]
    public async Task GetAll_ReturnsSeededCustomer()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.IssueJwt(Roles.BankEmployee));

        var response = await client.GetAsync("/api/customers?page=1&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CustomerSummaryDto>>(IntegrationTestJsonOptions.Default);

        Assert.NotNull(result);
        Assert.Contains(result!.Items, c => c.Id == _factory.SeededCustomerId);
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
