using MiniErp.Application.Common;
using MiniErp.Domain.Customers;

namespace MiniErp.Application.Customers;

/// <summary>Deletes a customer permanently, unless it has invoices (US4, FR-008, FR-009).</summary>
public sealed class DeleteCustomer(ICustomerRepository repository, ICustomerInvoiceChecker invoiceChecker)
{
    public async Task<Result> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        Customer? customer = await repository.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return Error.NotFound(CustomerErrors.NotFoundTitle);
        }

        if (await invoiceChecker.HasInvoicesAsync(id, cancellationToken))
        {
            return Error.Conflict(CustomerErrors.HasInvoicesTitle);
        }

        await repository.DeleteAsync(id, cancellationToken);

        return Result.Success();
    }
}
