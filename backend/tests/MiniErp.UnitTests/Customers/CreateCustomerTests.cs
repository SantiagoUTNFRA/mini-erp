using MiniErp.Application.Common;
using MiniErp.Application.Customers;
using MiniErp.Domain.Customers;
using MiniErp.UnitTests.Customers.Fakes;

namespace MiniErp.UnitTests.Customers;

public class CreateCustomerTests
{
    private readonly InMemoryCustomerRepository _repository = new();

    private static CustomerData ValidData(string taxId = "20-12345678-6") => new(
        LegalName: "Pérez, Juan",
        TaxId: taxId,
        VatCondition: "finalConsumer",
        Email: "juan@example.com",
        Phone: null,
        Address: null);

    [Fact]
    public async Task ExecuteAsync_ValidData_StoresAndReturnsCustomer()
    {
        CreateCustomer createCustomer = new(_repository);

        Result<Customer> result = await createCustomer.ExecuteAsync(ValidData(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Customer stored = Assert.Single(_repository.Stored);
        Assert.Equal(result.Value.Id, stored.Id);
        Assert.Equal("20123456786", stored.TaxId.Value);
    }

    [Fact]
    public async Task ExecuteAsync_TaxIdAlreadyRegistered_ReturnsConflictAndStoresNothing()
    {
        _repository.Seed(Customer.Create(ValidData("20123456786")).Value);
        CreateCustomer createCustomer = new(_repository);

        Result<Customer> result = await createCustomer.ExecuteAsync(ValidData("20-12345678-6"), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.Conflict, result.Error?.Kind);
        Assert.Equal(CustomerErrors.AlreadyExistsTitle, result.Error?.Title);
        Assert.Single(_repository.Stored);
    }

    [Fact]
    public async Task ExecuteAsync_StorageRejectsDuplicate_ReturnsConflict()
    {
        _repository.RejectWritesAsDuplicate = true;
        CreateCustomer createCustomer = new(_repository);

        Result<Customer> result = await createCustomer.ExecuteAsync(ValidData(), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.Conflict, result.Error?.Kind);
        Assert.Equal(CustomerErrors.AlreadyExistsTitle, result.Error?.Title);
        Assert.Empty(_repository.Stored);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidData_ReturnsAllFieldErrorsAndStoresNothing()
    {
        CreateCustomer createCustomer = new(_repository);
        CustomerData data = ValidData("20-12345678-5") with { LegalName = null, Email = "foo" };

        Result<Customer> result = await createCustomer.ExecuteAsync(data, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.Validation, result.Error?.Kind);
        Assert.Equal(
            [nameof(Customer.Email), nameof(Customer.LegalName), nameof(Customer.TaxId)],
            result.Error?.FieldErrors.Select(error => error.Field).Order());
        Assert.Empty(_repository.Stored);
    }
}
