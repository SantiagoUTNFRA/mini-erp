using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using MiniErp.Api.Customers;
using MiniErp.Domain.Customers;
using MiniErp.IntegrationTests.Infrastructure;

namespace MiniErp.IntegrationTests.Customers;

public class UpdateCustomerEndpointTests(MiniErpApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    private readonly Customer _juan = Customer.Create(new CustomerData(
        LegalName: "Pérez, Juan",
        TaxId: "20-12345678-6",
        VatCondition: "finalConsumer",
        Email: "juan@example.com",
        Phone: "+54 11 4321-5678",
        Address: "Av. Siempre Viva 742")).Value;

    private readonly Customer _gomez = TestCustomers.Create("Gómez SA", "30-71234567-1");

    public async ValueTask InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        await factory.SeedAsync(_juan, _gomez);
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    private static CustomerRequest Request(string taxId = "20-12345678-6") => new(
        LegalName: "Pérez, Juan Carlos",
        TaxId: taxId,
        VatCondition: "registeredTaxpayer",
        Email: "juancarlos@example.com",
        Phone: null,
        Address: "Calle Falsa 123");

    [Fact]
    public async Task Put_ValidData_ReturnsOkWithReplacedDataAndAbsentPhone()
    {
        HttpResponseMessage response = await PutAsync(_juan.Id, Request());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        CustomerResponse? body = await response.Content.ReadFromJsonAsync<CustomerResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(
            new CustomerResponse(
                _juan.Id,
                "Pérez, Juan Carlos",
                "20123456786",
                "registeredTaxpayer",
                "juancarlos@example.com",
                null,
                "Calle Falsa 123"),
            body);
        Assert.Equal(body, await GetAsync(_juan.Id));
    }

    [Fact]
    public async Task Put_ChangesToAnUnusedTaxId_ReturnsOk()
    {
        HttpResponseMessage response = await PutAsync(_juan.Id, Request("20-30000007-0"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("20300000070", (await GetAsync(_juan.Id)).TaxId);
    }

    [Fact]
    public async Task Put_UnknownId_ReturnsNotFound()
    {
        HttpResponseMessage response = await PutAsync(Guid.CreateVersion7(), Request());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_TaxIdOfAnotherCustomer_ReturnsConflictAndLeavesCustomerUnchanged()
    {
        CustomerResponse before = await GetAsync(_juan.Id);

        HttpResponseMessage response = await PutAsync(_juan.Id, Request("30-71234567-1"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("Customer already exists", problem.GetProperty("title").GetString());
        Assert.Equal(before, await GetAsync(_juan.Id));
    }

    [Fact]
    public async Task Put_SeveralInvalidFields_ReturnsBadRequestAndLeavesCustomerUnchanged()
    {
        CustomerResponse before = await GetAsync(_juan.Id);
        CustomerRequest invalid = Request("20-12345678-5") with { LegalName = "  ", Email = "foo", Phone = "abc" };

        HttpResponseMessage response = await PutAsync(_juan.Id, invalid);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(
            ["email", "legalName", "phone", "taxId"],
            problem.GetProperty("errors").EnumerateObject().Select(error => error.Name).Order());
        Assert.Equal(before, await GetAsync(_juan.Id));
    }

    private Task<HttpResponseMessage> PutAsync(Guid id, CustomerRequest request) =>
        _client.PutAsJsonAsync($"/api/customers/{id}", request, TestContext.Current.CancellationToken);

    private async Task<CustomerResponse> GetAsync(Guid id)
    {
        CustomerResponse? customer = await _client.GetFromJsonAsync<CustomerResponse>(
            $"/api/customers/{id}", TestContext.Current.CancellationToken);
        Assert.NotNull(customer);
        return customer;
    }
}
