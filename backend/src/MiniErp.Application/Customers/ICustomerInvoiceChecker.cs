namespace MiniErp.Application.Customers;

/// <summary>Tells whether a customer has invoices, which prevents deleting it (FR-009).</summary>
public interface ICustomerInvoiceChecker
{
    Task<bool> HasInvoicesAsync(Guid customerId, CancellationToken cancellationToken);
}
