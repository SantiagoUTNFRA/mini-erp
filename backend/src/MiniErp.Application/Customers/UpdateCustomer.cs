using MiniErp.Application.Common;
using MiniErp.Domain.Common;
using MiniErp.Domain.Customers;

namespace MiniErp.Application.Customers;

/// <summary>Replaces all the data of an existing customer (US3, FR-007).</summary>
public sealed class UpdateCustomer(ICustomerRepository repository)
{
    public async Task<Result<Customer>> ExecuteAsync(Guid id, CustomerData data, CancellationToken cancellationToken)
    {
        Customer? customer = await repository.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return Error.NotFound(CustomerErrors.NotFoundTitle);
        }

        // Changes only the loaded copy; nothing is stored until UpdateAsync succeeds (FR-011).
        Validated<Customer> updated = customer.Update(data);
        if (!updated.IsValid)
        {
            return Error.Validation(updated.Errors);
        }

        // Keeping its own tax id is not a duplicate.
        if (await repository.ExistsByTaxIdAsync(customer.TaxId, excludingId: customer.Id, cancellationToken))
        {
            return Error.Conflict(CustomerErrors.AlreadyExistsTitle);
        }

        try
        {
            await repository.UpdateAsync(customer, cancellationToken);
        }
        catch (DuplicateTaxIdException)
        {
            return Error.Conflict(CustomerErrors.AlreadyExistsTitle);
        }

        return customer;
    }
}
