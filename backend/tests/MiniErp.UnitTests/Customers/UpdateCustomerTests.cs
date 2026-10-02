using MiniErp.Application.Common;
using MiniErp.Application.Customers;
using MiniErp.Domain.Customers;
using MiniErp.UnitTests.Customers.Fakes;

namespace MiniErp.UnitTests.Customers;

public class UpdateCustomerTests
{
    private readonly InMemoryCustomerRepository _repository = new();
    private readonly Customer _juan = Customer.Create(Data("Pérez, Juan", "20-12345678-6", phone: "4321-5678")).Value;
    private readonly Customer _gomez = Customer.Create(Data("Gómez SA", "30-71234567-1")).Value;

    public UpdateCustomerTests() => _repository.Seed(_juan, _gomez);

    private static CustomerData Data(string legalName, string taxId, string? phone = null) => new(
        LegalName: legalName,
        TaxId: taxId,
        VatCondition: "finalConsumer",
        Email: "contacto@example.com",
        Phone: phone,
        Address: null);

    private Customer StoredJuan => _repository.Stored.Single(customer => customer.Id == _juan.Id);

    [Fact]
    public async Task ExecuteAsync_ValidData_ReplacesAllValues()
    {
        UpdateCustomer updateCustomer = new(_repository);

        Result<Customer> result = await updateCustomer.ExecuteAsync(
            _juan.Id, Data("Pérez, Juan Carlos", "20-12345678-6"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Pérez, Juan Carlos", StoredJuan.LegalName.Value);
        Assert.Null(StoredJuan.Phone); // not sent: full replacement leaves it absent
    }

    [Fact]
    public async Task ExecuteAsync_UnknownId_ReturnsNotFound()
    {
        UpdateCustomer updateCustomer = new(_repository);

        Result<Customer> result = await updateCustomer.ExecuteAsync(
            Guid.CreateVersion7(), Data("Nadie", "20-12345678-6"), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.NotFound, result.Error?.Kind);
        Assert.Equal(CustomerErrors.NotFoundTitle, result.Error?.Title);
    }

    [Fact]
    public async Task ExecuteAsync_TaxIdOfAnotherCustomer_ReturnsConflictAndLeavesCustomerUnchanged()
    {
        UpdateCustomer updateCustomer = new(_repository);

        Result<Customer> result = await updateCustomer.ExecuteAsync(
            _juan.Id, Data("Pérez, Juan", "30-71234567-1"), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.Conflict, result.Error?.Kind);
        Assert.Equal(CustomerErrors.AlreadyExistsTitle, result.Error?.Title);
        Assert.Equal("20123456786", StoredJuan.TaxId.Value);
        Assert.Equal("4321-5678", StoredJuan.Phone?.Value);
    }

    [Fact]
    public async Task ExecuteAsync_KeepsItsOwnTaxId_IsNotADuplicate()
    {
        UpdateCustomer updateCustomer = new(_repository);

        Result<Customer> result = await updateCustomer.ExecuteAsync(
            _juan.Id, Data("Pérez, Juan", "20123456786", phone: "4321-5678"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_ChangesToAnUnusedTaxId_Succeeds()
    {
        UpdateCustomer updateCustomer = new(_repository);

        Result<Customer> result = await updateCustomer.ExecuteAsync(
            _juan.Id, Data("Pérez, Juan", "20-30000007-0"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("20300000070", StoredJuan.TaxId.Value);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidData_ReturnsValidationAndLeavesCustomerUnchanged()
    {
        UpdateCustomer updateCustomer = new(_repository);
        CustomerData invalid = Data("Otro nombre", "20-12345678-5") with { Email = "foo" };

        Result<Customer> result = await updateCustomer.ExecuteAsync(_juan.Id, invalid, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.Validation, result.Error?.Kind);
        Assert.Equal("Pérez, Juan", StoredJuan.LegalName.Value);
    }

    [Fact]
    public async Task ExecuteAsync_StorageRejectsDuplicate_ReturnsConflict()
    {
        _repository.RejectWritesAsDuplicate = true;
        UpdateCustomer updateCustomer = new(_repository);

        Result<Customer> result = await updateCustomer.ExecuteAsync(
            _juan.Id, Data("Pérez, Juan Carlos", "20-12345678-6"), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.Conflict, result.Error?.Kind);
        Assert.Equal("Pérez, Juan", StoredJuan.LegalName.Value);
    }
}
