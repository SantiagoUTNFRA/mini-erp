using MiniErp.Application.Common;
using MiniErp.Domain.Customers;

namespace MiniErp.Application.Customers;

/// <summary>Returns one customer by id (US2).</summary>
public sealed class GetCustomer(ICustomerRepository repository)
{
    public async Task<Result<Customer>> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        Customer? customer = await repository.GetByIdAsync(id, cancellationToken);

        return customer is null ? Error.NotFound(CustomerErrors.NotFoundTitle) : customer;
    }
}
