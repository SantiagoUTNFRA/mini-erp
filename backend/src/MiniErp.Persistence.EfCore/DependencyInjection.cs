using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using MiniErp.Application.Customers;
using MiniErp.Persistence.EfCore.Customers;

namespace MiniErp.Persistence.EfCore;

public static class DependencyInjection
{
    /// <summary>Registers the EF Core + MySQL implementation of the persistence ports.</summary>
    public static IServiceCollection AddEfCorePersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<MiniErpDbContext>(options => options.UseMySQL(connectionString));
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerInvoiceChecker, EfCoreCustomerInvoiceChecker>();

        return services;
    }

    /// <summary>Applies pending migrations. Meant for Development and tests only (research R9).</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        MiniErpDbContext dbContext = scope.ServiceProvider.GetRequiredService<MiniErpDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
