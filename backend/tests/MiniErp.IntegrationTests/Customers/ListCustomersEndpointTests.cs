using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using MiniErp.Api.Customers;
using MiniErp.Domain.Customers;
using MiniErp.IntegrationTests.Infrastructure;

namespace MiniErp.IntegrationTests.Customers;

public class ListCustomersEndpointTests(MiniErpApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async ValueTask InitializeAsync() => await factory.ResetDatabaseAsync();

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Get_SeveralCustomers_ReturnsThemOrderedByLegalNameWithTotal()
    {
        // No case or accent variants, so the result does not depend on the database collation.
        await factory.SeedAsync(
            TestCustomers.Create("Gamma SA", index: 1),
            TestCustomers.Create("Alfa SA", index: 2),
            TestCustomers.Create("Beta SRL", index: 3));

        CustomerPageResponse page = await GetPageAsync("/api/customers");

        Assert.Equal(["Alfa SA", "Beta SRL", "Gamma SA"], page.Items.Select(customer => customer.LegalName));
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(1, page.Page);
    }

    [Fact]
    public async Task Get_NoPageSizeGiven_ReturnsTwentyItems()
    {
        Customer[] customers = [.. Enumerable.Range(0, 21).Select(i => TestCustomers.Create($"Cliente {i:D2}", index: i))];
        await factory.SeedAsync(customers);

        CustomerPageResponse page = await GetPageAsync("/api/customers");

        Assert.Equal(20, page.PageSize);
        Assert.Equal(20, page.Items.Count);
        Assert.Equal(21, page.TotalCount);
    }

    [Fact]
    public async Task Get_SecondPage_ReturnsTheRemainingItems()
    {
        await factory.SeedAsync(
            TestCustomers.Create("Alfa SA", index: 1),
            TestCustomers.Create("Beta SRL", index: 2),
            TestCustomers.Create("Gamma SA", index: 3));

        CustomerPageResponse page = await GetPageAsync("/api/customers?page=2&pageSize=2");

        CustomerResponse only = Assert.Single(page.Items);
        Assert.Equal("Gamma SA", only.LegalName);
        Assert.Equal(3, page.TotalCount);
    }

    [Fact]
    public async Task Get_PageOutOfRange_ReturnsOkWithNoItems()
    {
        await factory.SeedAsync(TestCustomers.Create("Alfa SA", index: 1));

        CustomerPageResponse page = await GetPageAsync("/api/customers?page=50");

        Assert.Empty(page.Items);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task Get_NoCustomers_ReturnsOkWithEmptyList()
    {
        CustomerPageResponse page = await GetPageAsync("/api/customers");

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Theory]
    [InlineData("/api/customers?pageSize=101", "pageSize")]
    [InlineData("/api/customers?pageSize=0", "pageSize")]
    [InlineData("/api/customers?page=0", "page")]
    public async Task Get_InvalidPaging_ReturnsBadRequestForThatField(string url, string field)
    {
        HttpResponseMessage response = await _client.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal([field], problem.GetProperty("errors").EnumerateObject().Select(error => error.Name));
    }

    private async Task<CustomerPageResponse> GetPageAsync(string url)
    {
        HttpResponseMessage response = await _client.GetAsync(url, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        CustomerPageResponse? page = await response.Content.ReadFromJsonAsync<CustomerPageResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(page);
        return page;
    }
}
