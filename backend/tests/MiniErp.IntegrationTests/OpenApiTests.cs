using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;

namespace MiniErp.IntegrationTests;

public class OpenApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetOpenApiDocument_InDevelopment_ReturnsOk()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
