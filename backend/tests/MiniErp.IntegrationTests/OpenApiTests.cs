using System.Net;

using MiniErp.IntegrationTests.Infrastructure;

namespace MiniErp.IntegrationTests;

public class OpenApiTests(MiniErpApiFactory factory)
{
    [Fact]
    public async Task GetOpenApiDocument_InDevelopment_ReturnsOk()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
