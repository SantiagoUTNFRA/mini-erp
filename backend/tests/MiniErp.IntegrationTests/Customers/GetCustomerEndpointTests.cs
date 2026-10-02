using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using MiniErp.Api.Customers;
using MiniErp.Domain.Customers;
using MiniErp.IntegrationTests.Infrastructure;

namespace MiniErp.IntegrationTests.Customers;

public class GetCustomerEndpointTests(MiniErpApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async ValueTask InitializeAsync() => await factory.ResetDatabaseAsync();

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Get_ExistingCustomer_ReturnsAllItsData()
    {
        Customer customer = Customer.Create(new CustomerData(
            LegalName: "Pérez, Juan",
            TaxId: "20-12345678-6",
            VatCondition: "registeredTaxpayer",
            Email: "juan@example.com",
            Phone: "+54 11 4321-5678",
            Address: "Av. Siempre Viva 742")).Value;
        await factory.SeedAsync(customer);

        HttpResponseMessage response = await _client.GetAsync($"/api/customers/{customer.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        CustomerResponse? body = await response.Content.ReadFromJsonAsync<CustomerResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(
            new CustomerResponse(
                customer.Id,
                "Pérez, Juan",
                "20123456786",
                "registeredTaxpayer",
                "juan@example.com",
                "+54 11 4321-5678",
                "Av. Siempre Viva 742"),
            body);
    }

    [Fact]
    public async Task Get_OptionalFieldsAbsent_ReturnsThemAsNull()
    {
        Customer customer = TestCustomers.Create("Alfa SA", index: 1);
        await factory.SeedAsync(customer);

        HttpResponseMessage response = await _client.GetAsync($"/api/customers/{customer.Id}", TestContext.Current.CancellationToken);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("email").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("phone").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("address").ValueKind);
    }

    [Fact]
    public async Task Get_UnknownId_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/customers/{Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("Customer not found", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Get_MalformedId_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/customers/not-a-guid", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
