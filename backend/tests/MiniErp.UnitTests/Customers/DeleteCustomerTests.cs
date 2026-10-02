using MiniErp.Application.Common;
using MiniErp.Application.Customers;
using MiniErp.Domain.Customers;
using MiniErp.UnitTests.Customers.Fakes;

namespace MiniErp.UnitTests.Customers;

public class DeleteCustomerTests
{
    private readonly InMemoryCustomerRepository _repository = new();
    private readonly FakeCustomerInvoiceChecker _invoiceChecker = new();
    private readonly Customer _customer =
        Customer.Create(new CustomerData("Pérez, Juan", "20-12345678-6", "finalConsumer", null, null, null)).Value;

    public DeleteCustomerTests() => _repository.Seed(_customer);

    [Fact]
    public async Task ExecuteAsync_CustomerWithoutInvoices_DeletesIt()
    {
        DeleteCustomer deleteCustomer = new(_repository, _invoiceChecker);

        Result result = await deleteCustomer.ExecuteAsync(_customer.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Empty(_repository.Stored);
    }

    [Fact]
    public async Task ExecuteAsync_CustomerWithInvoices_ReturnsConflictAndKeepsIt()
    {
        _invoiceChecker.HasInvoices = true;
        DeleteCustomer deleteCustomer = new(_repository, _invoiceChecker);

        Result result = await deleteCustomer.ExecuteAsync(_customer.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.Conflict, result.Error?.Kind);
        Assert.Equal(CustomerErrors.HasInvoicesTitle, result.Error?.Title);
        Assert.Single(_repository.Stored);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownId_ReturnsNotFound()
    {
        DeleteCustomer deleteCustomer = new(_repository, _invoiceChecker);

        Result result = await deleteCustomer.ExecuteAsync(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.NotFound, result.Error?.Kind);
        Assert.Single(_repository.Stored);
    }
}
