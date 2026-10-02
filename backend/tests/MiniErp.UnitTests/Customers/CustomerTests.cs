using MiniErp.Domain.Common;
using MiniErp.Domain.Customers;

namespace MiniErp.UnitTests.Customers;

public class CustomerTests
{
    private static CustomerData ValidData() => new(
        LegalName: "Pérez, Juan",
        TaxId: "20-12345678-6",
        VatCondition: "finalConsumer",
        Email: "juan@example.com",
        Phone: "+54 11 4321-5678",
        Address: "Av. Siempre Viva 742");

    [Fact]
    public void Create_ValidData_AssignsVersion7Id()
    {
        Validated<Customer> result = Customer.Create(ValidData());

        Assert.True(result.IsValid);
        Assert.Equal(7, result.Value.Id.Version);
    }

    [Fact]
    public void Create_ValidData_NormalizesValues()
    {
        Customer customer = Customer.Create(ValidData()).Value;

        Assert.Equal("Pérez, Juan", customer.LegalName.Value);
        Assert.Equal("20123456786", customer.TaxId.Value);
        Assert.Equal(VatCondition.FinalConsumer, customer.VatCondition);
        Assert.Equal("juan@example.com", customer.Email?.Value);
        Assert.Equal("+54 11 4321-5678", customer.Phone?.Value);
        Assert.Equal("Av. Siempre Viva 742", customer.Address?.Value);
    }

    [Fact]
    public void Create_SeveralInvalidFields_ReturnsAllErrorsTogether()
    {
        CustomerData data = ValidData() with
        {
            LegalName = null,
            TaxId = "20-12345678-5",
            VatCondition = "unknown",
            Email = "foo",
        };

        Validated<Customer> result = Customer.Create(data);

        Assert.False(result.IsValid);
        Assert.Equal(
            [nameof(Customer.Email), nameof(Customer.LegalName), nameof(Customer.TaxId), nameof(Customer.VatCondition)],
            result.Errors.Select(error => error.Field).Order());
    }

    [Theory]
    [InlineData("registeredTaxpayer", VatCondition.RegisteredTaxpayer)]
    [InlineData("simplifiedRegime", VatCondition.SimplifiedRegime)]
    [InlineData("exempt", VatCondition.Exempt)]
    [InlineData("finalConsumer", VatCondition.FinalConsumer)]
    [InlineData("FINALCONSUMER", VatCondition.FinalConsumer)]
    [InlineData(" exempt ", VatCondition.Exempt)]
    public void Create_KnownVatCondition_IgnoresCase(string raw, VatCondition expected)
    {
        Customer customer = Customer.Create(ValidData() with { VatCondition = raw }).Value;

        Assert.Equal(expected, customer.VatCondition);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("monotributista")]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("exempt,finalConsumer")]
    public void Create_UnknownOrNumericVatCondition_ReturnsVatConditionError(string? raw)
    {
        Validated<Customer> result = Customer.Create(ValidData() with { VatCondition = raw });

        FieldError error = Assert.Single(result.Errors);
        Assert.Equal(nameof(Customer.VatCondition), error.Field);
    }

    [Fact]
    public void Create_OptionalFieldsBlankOrWhitespace_AreAbsent()
    {
        CustomerData data = ValidData() with { Email = "", Phone = "   ", Address = null };

        Customer customer = Customer.Create(data).Value;

        Assert.Null(customer.Email);
        Assert.Null(customer.Phone);
        Assert.Null(customer.Address);
    }

    [Fact]
    public void Update_ValidData_ReplacesAllValuesAndKeepsId()
    {
        Customer customer = Customer.Create(ValidData()).Value;
        Guid originalId = customer.Id;
        CustomerData newData = new(
            LegalName: "Gómez SA",
            TaxId: "30-71234567-1",
            VatCondition: "registeredTaxpayer",
            Email: null,
            Phone: null,
            Address: "Calle Falsa 123");

        Validated<Customer> result = customer.Update(newData);

        Assert.True(result.IsValid);
        Assert.Equal(originalId, customer.Id);
        Assert.Equal("Gómez SA", customer.LegalName.Value);
        Assert.Equal("30712345671", customer.TaxId.Value);
        Assert.Equal(VatCondition.RegisteredTaxpayer, customer.VatCondition);
        Assert.Null(customer.Email);
        Assert.Null(customer.Phone);
        Assert.Equal("Calle Falsa 123", customer.Address?.Value);
    }

    [Fact]
    public void Update_InvalidData_ReturnsErrorsAndLeavesCustomerUnchanged()
    {
        Customer customer = Customer.Create(ValidData()).Value;

        Validated<Customer> result = customer.Update(ValidData() with { LegalName = "Otro nombre", Email = "foo" });

        Assert.False(result.IsValid);
        Assert.Equal("Pérez, Juan", customer.LegalName.Value);
        Assert.Equal("juan@example.com", customer.Email?.Value);
    }
}
