using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MiniErp.Persistence.EfCore;

/// <summary>
/// Used only by the dotnet-ef tool, so migrations can be generated without starting MiniErp.Api.
/// `migrations add` does not connect to the database; commands that do (e.g. `database update`) read
/// the connection string from MINIERP_DESIGN_CONNECTION_STRING.
/// </summary>
internal sealed class MiniErpDbContextFactory : IDesignTimeDbContextFactory<MiniErpDbContext>
{
    private const string ConnectionStringVariable = "MINIERP_DESIGN_CONNECTION_STRING";
    private const string PlaceholderConnectionString = "Server=127.0.0.1;Port=3306;Database=minierp";

    public MiniErpDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable)
            ?? PlaceholderConnectionString;

        DbContextOptions<MiniErpDbContext> options = new DbContextOptionsBuilder<MiniErpDbContext>()
            .UseMySQL(connectionString)
            .Options;

        return new MiniErpDbContext(options);
    }
}
