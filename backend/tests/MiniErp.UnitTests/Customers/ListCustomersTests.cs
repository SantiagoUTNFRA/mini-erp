using MiniErp.Application.Common;
using MiniErp.Application.Customers;
using MiniErp.Domain.Customers;
using MiniErp.UnitTests.Customers.Fakes;

namespace MiniErp.UnitTests.Customers;

public class ListCustomersTests
{
    private readonly InMemoryCustomerRepository _repository = new();

    [Fact]
    public async Task ExecuteAsync_NoPagingGiven_UsesPageOneAndSizeTwenty()
    {
        ListCustomers listCustomers = new(_repository);

        Result<CustomerPageResult> result = await listCustomers.ExecuteAsync(null, null, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(20, result.Value.PageSize);
    }

    [Fact]
    public async Task ExecuteAsync_CustomersStored_ReturnsPageAndTotal()
    {
        _repository.Seed(
            Customer.Create(new CustomerData("Beta SRL", "30-71234567-1", "exempt", null, null, null)).Value,
            Customer.Create(new CustomerData("Alfa SA", "20-12345678-6", "exempt", null, null, null)).Value);
        ListCustomers listCustomers = new(_repository);

        Result<CustomerPageResult> result = await listCustomers.ExecuteAsync(1, 1, TestContext.Current.CancellationToken);

        Customer only = Assert.Single(result.Value.Items);
        Assert.Equal("Alfa SA", only.LegalName.Value);
        Assert.Equal(2, result.Value.TotalCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ExecuteAsync_PageBelowOne_ReturnsPageValidationError(int page)
    {
        ListCustomers listCustomers = new(_repository);

        Result<CustomerPageResult> result = await listCustomers.ExecuteAsync(page, null, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.Validation, result.Error?.Kind);
        Assert.Equal(["Page"], result.Error?.FieldErrors.Select(error => error.Field));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task ExecuteAsync_PageSizeOutOfRange_ReturnsPageSizeValidationError(int pageSize)
    {
        ListCustomers listCustomers = new(_repository);

        Result<CustomerPageResult> result = await listCustomers.ExecuteAsync(null, pageSize, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.Validation, result.Error?.Kind);
        Assert.Equal(["PageSize"], result.Error?.FieldErrors.Select(error => error.Field));
    }

    [Fact]
    public async Task ExecuteAsync_MaxPageSize_IsAllowed()
    {
        ListCustomers listCustomers = new(_repository);

        Result<CustomerPageResult> result = await listCustomers.ExecuteAsync(1, 100, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }
}
