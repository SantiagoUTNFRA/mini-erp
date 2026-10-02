using System.Diagnostics;
using System.Net;

using Microsoft.Extensions.DependencyInjection;

using MiniErp.IntegrationTests.Infrastructure;
using MiniErp.Persistence.EfCore;
using MiniErp.Persistence.EfCore.Customers;

namespace MiniErp.IntegrationTests.Customers;

/// <summary>
/// SC-005: with 10,000 customers, list and detail respond in under a second. Explicit (it seeds
/// 10,000 rows), so it does not run in a plain `dotnet test`; run it with `-- --explicit only`.
/// </summary>
public class CustomerPerformanceTests(MiniErpApiFactory factory) : IAsyncLifetime
{
    private const int CustomerCount = 10_000;
    private static readonly TimeSpan MaxResponseTime = TimeSpan.FromSeconds(1);

    private readonly HttpClient _client = factory.CreateClient();

    public async ValueTask InitializeAsync() => await factory.ResetDatabaseAsync();

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await factory.ResetDatabaseAsync();
    }

    [Fact(Explicit = true)]
    public async Task ListAndDetail_TenThousandCustomers_RespondInUnderOneSecond()
    {
        Guid lastId = await SeedCustomersAsync();

        // Warm-up: measure steady state, not JIT compilation of the first request.
        await _client.GetAsync("/api/customers", TestContext.Current.CancellationToken);

        TimeSpan listTime = await MeasureAsync("/api/customers?page=250&pageSize=40");
        TimeSpan detailTime = await MeasureAsync($"/api/customers/{lastId}");
        TestContext.Current.TestOutputHelper?.WriteLine(
            $"List: {listTime.TotalMilliseconds:F1} ms; detail: {detailTime.TotalMilliseconds:F1} ms.");

        Assert.True(listTime < MaxResponseTime, $"List took {listTime.TotalMilliseconds} ms.");
        Assert.True(detailTime < MaxResponseTime, $"Detail took {detailTime.TotalMilliseconds} ms.");
    }

    // Bulk insert through the DbContext in a single SaveChanges: going through the API would take
    // 10,000 requests. Data is valid, because reading maps every row back through the domain.
    private async Task<Guid> SeedCustomersAsync()
    {
        CustomerRecord[] records =
        [
            .. Enumerable.Range(0, CustomerCount).Select(i => new CustomerRecord
            {
                Id = Guid.CreateVersion7(),
                LegalName = $"Cliente {i:D5}",
                TaxId = TestCustomers.ValidTaxId(i),
                VatCondition = "FinalConsumer",
            }),
        ];

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        MiniErpDbContext dbContext = scope.ServiceProvider.GetRequiredService<MiniErpDbContext>();
        dbContext.Customers.AddRange(records);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return records[^1].Id;
    }

    private async Task<TimeSpan> MeasureAsync(string url)
    {
        long start = Stopwatch.GetTimestamp();
        HttpResponseMessage response = await _client.GetAsync(url, TestContext.Current.CancellationToken);
        TimeSpan elapsed = Stopwatch.GetElapsedTime(start);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return elapsed;
    }
}
