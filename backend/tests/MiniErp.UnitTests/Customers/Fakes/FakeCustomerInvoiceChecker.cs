using MiniErp.Application.Customers;

namespace MiniErp.UnitTests.Customers.Fakes;

public sealed class FakeCustomerInvoiceChecker : ICustomerInvoiceChecker
{
    public bool HasInvoices { get; set; }

    public Task<bool> HasInvoicesAsync(Guid customerId, CancellationToken cancellationToken) =>
        Task.FromResult(HasInvoices);
}
