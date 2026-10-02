using MiniErp.Application.Customers;

namespace MiniErp.Persistence.EfCore.Customers;

internal sealed class EfCoreCustomerInvoiceChecker : ICustomerInvoiceChecker
{
    // Invoicing does not exist yet, so no customer has invoices. The invoicing feature replaces this
    // with a real query; the delete use case already enforces the rule (research R8).
    public Task<bool> HasInvoicesAsync(Guid customerId, CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
