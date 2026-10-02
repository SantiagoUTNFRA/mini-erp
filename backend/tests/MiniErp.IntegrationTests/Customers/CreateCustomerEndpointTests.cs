using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using MiniErp.Api.Customers;
using MiniErp.IntegrationTests.Infrastructure;

namespace MiniErp.IntegrationTests.Customers;

public class CreateCustomerEndpointTests(MiniErpApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async ValueTask InitializeAsync() => await factory.ResetDatabaseAsync();

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    private static CustomerRequest ValidRequest(string taxId = "20-12345678-6") => new(
        LegalName: "Pérez, Juan",
        TaxId: taxId,
        VatCondition: "finalConsumer",
        Email: "juan@example.com",
        Phone: "+54 11 4321-5678",
        Address: "Av. Siempre Viva 742");

    [Fact]
    public async Task Post_ValidCustomer_ReturnsCreatedWithLocationAndNormalizedTaxId()
    {
        HttpResponseMessage response = await PostAsync(ValidRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        CustomerResponse? customer = await response.Content.ReadFromJsonAsync<CustomerResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(customer);
        Assert.Equal($"/api/customers/{customer.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal("20123456786", customer.TaxId);
        Assert.Equal("finalConsumer", customer.VatCondition);
        Assert.Equal("Pérez, Juan", customer.LegalName);
    }

    [Fact]
    public async Task Post_SameTaxIdWithoutSeparators_ReturnsConflict()
    {
        await PostAsync(ValidRequest("20-12345678-6"));

        HttpResponseMessage response = await PostAsync(ValidRequest("20123456786"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        JsonElement problem = await ReadJsonAsync(response);
        Assert.Equal("Customer already exists", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Post_SeveralInvalidFields_ReturnsOneBadRequestWithAllOfThem()
    {
        CustomerRequest request = ValidRequest("20-12345678-5") with { LegalName = null, Email = "foo" };

        HttpResponseMessage response = await PostAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["email", "legalName", "taxId"], await ReadErrorKeysAsync(response));
    }

    [Fact]
    public async Task Post_UnknownVatCondition_ReportsItAsAFieldError()
    {
        HttpResponseMessage response = await PostAsync(ValidRequest() with { VatCondition = "monotributista" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["vatCondition"], await ReadErrorKeysAsync(response));
    }

    [Fact]
    public async Task Post_LegalNameOverMaxLength_ReturnsBadRequest()
    {
        HttpResponseMessage response = await PostAsync(ValidRequest() with { LegalName = new string('a', 201) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["legalName"], await ReadErrorKeysAsync(response));
    }

    [Fact]
    public async Task Post_TwoSimultaneousRequestsWithSameTaxId_OnlyOneSucceeds()
    {
        HttpResponseMessage[] responses = await Task.WhenAll(
            PostAsync(ValidRequest("20-12345678-6")),
            PostAsync(ValidRequest("20123456786")));

        Assert.Equal(
            [HttpStatusCode.Created, HttpStatusCode.Conflict],
            responses.Select(response => response.StatusCode).Order());
    }

    private Task<HttpResponseMessage> PostAsync(CustomerRequest request) =>
        _client.PostAsJsonAsync("/api/customers", request, TestContext.Current.CancellationToken);

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

    private static async Task<string[]> ReadErrorKeysAsync(HttpResponseMessage response)
    {
        JsonElement problem = await ReadJsonAsync(response);

        return [.. problem.GetProperty("errors").EnumerateObject().Select(error => error.Name).Order()];
    }
}
