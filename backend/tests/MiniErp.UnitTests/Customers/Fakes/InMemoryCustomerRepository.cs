using MiniErp.Application.Customers;
using MiniErp.Domain.Customers;

namespace MiniErp.UnitTests.Customers.Fakes;

/// <summary>
/// In-memory <see cref="ICustomerRepository"/>. Stores and returns copies, like a real database, so a
/// use case that mutates a loaded customer does not change what is stored until it saves.
/// </summary>
public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly Dictionary<Guid, Customer> _customers = [];

    /// <summary>Simulates storage rejecting a duplicate tax id (a race between two requests).</summary>
    public bool RejectWritesAsDuplicate { get; set; }

    public IReadOnlyCollection<Customer> Stored => _customers.Values.Select(Copy).ToList();

    public void Seed(params Customer[] customers)
    {
        foreach (Customer customer in customers)
        {
            _customers[customer.Id] = Copy(customer);
        }
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_customers.TryGetValue(id, out Customer? customer) ? Copy(customer) : null);

    public Task<CustomerPage> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        List<Customer> items = _customers.Values
            .OrderBy(customer => customer.LegalName.Value, StringComparer.OrdinalIgnoreCase)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Copy)
            .ToList();

        return Task.FromResult(new CustomerPage(items, _customers.Count));
    }

    public Task<bool> ExistsByTaxIdAsync(TaxId taxId, Guid? excludingId, CancellationToken cancellationToken) =>
        Task.FromResult(_customers.Values.Any(customer => customer.TaxId == taxId && customer.Id != excludingId));

    public Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        ThrowIfRejectingWrites();
        _customers.Add(customer.Id, Copy(customer));
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Customer customer, CancellationToken cancellationToken)
    {
        ThrowIfRejectingWrites();
        _customers[customer.Id] = Copy(customer);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _customers.Remove(id);
        return Task.CompletedTask;
    }

    private static Customer Copy(Customer customer) => Customer.Restore(
        customer.Id,
        customer.LegalName,
        customer.TaxId,
        customer.VatCondition,
        customer.Email,
        customer.Phone,
        customer.Address);

    private void ThrowIfRejectingWrites()
    {
        if (RejectWritesAsDuplicate)
        {
            throw new DuplicateTaxIdException("Simulated duplicate tax id.");
        }
    }
}
