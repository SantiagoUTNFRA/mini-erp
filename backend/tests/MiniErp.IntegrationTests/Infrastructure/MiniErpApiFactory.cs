using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using MiniErp.Application.Customers;
using MiniErp.Domain.Customers;
using MiniErp.IntegrationTests.Infrastructure;
using MiniErp.Persistence.EfCore;

using Testcontainers.MySql;

[assembly: AssemblyFixture(typeof(MiniErpApiFactory))]

namespace MiniErp.IntegrationTests.Infrastructure;

/// <summary>
/// The API running against a throwaway MySQL container (ADR-0005). One container per test run,
/// shared by every test class as an assembly fixture.
/// </summary>
public sealed class MiniErpApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Same image as compose.yaml, so tests and development use the same engine and version.
    private readonly MySqlContainer _mySql = new MySqlBuilder("mysql:9.7").Build();

    public async ValueTask InitializeAsync() => await _mySql.StartAsync(TestContext.Current.CancellationToken);

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _mySql.DisposeAsync();
    }

    /// <summary>Empties the tables, so each test starts from a known state.</summary>
    public async Task ResetDatabaseAsync()
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        MiniErpDbContext dbContext = scope.ServiceProvider.GetRequiredService<MiniErpDbContext>();

        await dbContext.Customers.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Stores customers through the persistence port, without going through the HTTP API.</summary>
    public async Task SeedAsync(params Customer[] customers)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        ICustomerRepository repository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();

        foreach (Customer customer in customers)
        {
            await repository.AddAsync(customer, TestContext.Current.CancellationToken);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        // UseSetting, not ConfigureAppConfiguration: with minimal hosting the latter is applied after
        // Program.cs reads the connection string, so the API would never see the container's.
        builder.UseSetting("ConnectionStrings:MiniErp", _mySql.GetConnectionString());
}
