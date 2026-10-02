using System.Net;

using Microsoft.Extensions.DependencyInjection;

using MiniErp.Application.Customers;
using MiniErp.Domain.Customers;
using MiniErp.IntegrationTests.Infrastructure;

namespace MiniErp.IntegrationTests.Customers;

public class DeleteCustomerEndpointTests(MiniErpApiFactory factory) : IAsyncLifetime
{
    private const string TaxId = "20-12345678-6";

    private readonly HttpClient _client = factory.CreateClient();
    private readonly Customer _customer = TestCustomers.Create("Pérez, Juan", TaxId);

    public async ValueTask InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        await factory.SeedAsync(_customer);
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Delete_ExistingCustomer_ReturnsNoContentAndItIsGone()
    {
        HttpResponseMessage response = await DeleteAsync(_customer.Id);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        HttpResponseMessage detail = await _client.GetAsync($"/api/customers/{_customer.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        HttpResponseMessage response = await DeleteAsync(Guid.CreateVersion7());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingCustomer_FreesItsTaxIdForANewCustomer()
    {
        await DeleteAsync(_customer.Id);

        // Proves the unique index released the tax id. Registering through the API (US4-4) is
        // covered by quickstart step 11, to keep this story independent from US1.
        TaxId taxId = Domain.Customers.TaxId.Create(TaxId).Value;
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        ICustomerRepository repository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
        Assert.False(await repository.ExistsByTaxIdAsync(taxId, excludingId: null, TestContext.Current.CancellationToken));

        Customer newCustomer = TestCustomers.Create("Pérez, Juan", TaxId);
        await factory.SeedAsync(newCustomer);
        Assert.NotEqual(_customer.Id, newCustomer.Id);
    }

    private Task<HttpResponseMessage> DeleteAsync(Guid id) =>
        _client.DeleteAsync($"/api/customers/{id}", TestContext.Current.CancellationToken);
}
