using MiniErp.Application.Common;
using MiniErp.Application.Customers;
using MiniErp.Domain.Customers;
using MiniErp.UnitTests.Customers.Fakes;

namespace MiniErp.UnitTests.Customers;

public class GetCustomerTests
{
    private readonly InMemoryCustomerRepository _repository = new();

    [Fact]
    public async Task ExecuteAsync_ExistingCustomer_ReturnsIt()
    {
        Customer customer = Customer.Create(new CustomerData("Pérez, Juan", "20-12345678-6", "exempt", null, null, null)).Value;
        _repository.Seed(customer);
        GetCustomer getCustomer = new(_repository);

        Result<Customer> result = await getCustomer.ExecuteAsync(customer.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(customer.Id, result.Value.Id);
        Assert.Equal(VatCondition.Exempt, result.Value.VatCondition);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownId_ReturnsNotFound()
    {
        GetCustomer getCustomer = new(_repository);

        Result<Customer> result = await getCustomer.ExecuteAsync(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.NotFound, result.Error?.Kind);
        Assert.Equal(CustomerErrors.NotFoundTitle, result.Error?.Title);
    }
}
