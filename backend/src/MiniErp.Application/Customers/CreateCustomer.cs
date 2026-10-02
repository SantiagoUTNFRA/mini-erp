using MiniErp.Application.Common;
using MiniErp.Domain.Common;
using MiniErp.Domain.Customers;

namespace MiniErp.Application.Customers;

/// <summary>Registers a new customer (US1).</summary>
public sealed class CreateCustomer(ICustomerRepository repository)
{
    public async Task<Result<Customer>> ExecuteAsync(CustomerData data, CancellationToken cancellationToken)
    {
        Validated<Customer> created = Customer.Create(data);
        if (!created.IsValid)
        {
            return Error.Validation(created.Errors);
        }

        Customer customer = created.Value;

        // Friendly check first; the unique index in storage still catches concurrent requests (R7).
        if (await repository.ExistsByTaxIdAsync(customer.TaxId, excludingId: null, cancellationToken))
        {
            return Error.Conflict(CustomerErrors.AlreadyExistsTitle);
        }

        try
        {
            await repository.AddAsync(customer, cancellationToken);
        }
        catch (DuplicateTaxIdException)
        {
            return Error.Conflict(CustomerErrors.AlreadyExistsTitle);
        }

        return customer;
    }
}
