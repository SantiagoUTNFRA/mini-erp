using Microsoft.EntityFrameworkCore;

using MiniErp.Persistence.EfCore.Customers;

namespace MiniErp.Persistence.EfCore;

public sealed class MiniErpDbContext(DbContextOptions<MiniErpDbContext> options) : DbContext(options)
{
    public DbSet<CustomerRecord> Customers => Set<CustomerRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MiniErpDbContext).Assembly);
}
