using Microsoft.EntityFrameworkCore;

using MiniErp.Application.Customers;
using MiniErp.Domain.Customers;

using MySql.Data.MySqlClient;

namespace MiniErp.Persistence.EfCore.Customers;

internal sealed class CustomerRepository(MiniErpDbContext dbContext) : ICustomerRepository
{
    // MySQL ER_DUP_ENTRY: a unique index rejected the row.
    private const int DuplicateEntryErrorNumber = 1062;

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        CustomerRecord? record = await dbContext.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(customer => customer.Id == id, cancellationToken);

        return record is null ? null : CustomerMapping.ToDomain(record);
    }

    public async Task<CustomerPage> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        int totalCount = await dbContext.Customers.CountAsync(cancellationToken);

        // Id breaks ties so that pages are stable when legal names repeat.
        List<CustomerRecord> records = await dbContext.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.LegalName)
            .ThenBy(customer => customer.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new CustomerPage(records.Select(CustomerMapping.ToDomain).ToList(), totalCount);
    }

    public Task<bool> ExistsByTaxIdAsync(TaxId taxId, Guid? excludingId, CancellationToken cancellationToken)
    {
        string value = taxId.Value;

        return dbContext.Customers.AnyAsync(
            customer => customer.TaxId == value && (excludingId == null || customer.Id != excludingId),
            cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        dbContext.Customers.Add(CustomerMapping.ToRecord(customer));
        await SaveChangesAsync(customer, cancellationToken);
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken)
    {
        CustomerRecord record = await dbContext.Customers
            .SingleOrDefaultAsync(stored => stored.Id == customer.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {customer.Id} does not exist.");

        CustomerMapping.CopyTo(customer, record);
        await SaveChangesAsync(customer, cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Customers
            .Where(customer => customer.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

    private async Task SaveChangesAsync(Customer customer, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is MySqlException { Number: DuplicateEntryErrorNumber })
        {
            // Leave the context clean: the rejected entity must not be retried on a later save.
            dbContext.ChangeTracker.Clear();
            throw new DuplicateTaxIdException($"Tax id {customer.TaxId} is already registered.", exception);
        }
    }
}
