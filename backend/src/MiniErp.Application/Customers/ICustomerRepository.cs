using MiniErp.Domain.Customers;

namespace MiniErp.Application.Customers;

/// <summary>Persistence port for customers.</summary>
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>A page of customers ordered by legal name, plus the total count.</summary>
    Task<CustomerPage> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<bool> ExistsByTaxIdAsync(TaxId taxId, Guid? excludingId, CancellationToken cancellationToken);

    /// <exception cref="DuplicateTaxIdException">The tax id is already registered.</exception>
    Task AddAsync(Customer customer, CancellationToken cancellationToken);

    /// <exception cref="DuplicateTaxIdException">The tax id belongs to another customer.</exception>
    Task UpdateAsync(Customer customer, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
