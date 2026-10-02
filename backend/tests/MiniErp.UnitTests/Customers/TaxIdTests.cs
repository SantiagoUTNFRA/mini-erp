using MiniErp.Domain.Common;
using MiniErp.Domain.Customers;

namespace MiniErp.UnitTests.Customers;

public class TaxIdTests
{
    [Theory]
    [InlineData("20-12345678-6")]
    [InlineData("20 12345678 6")]
    [InlineData("20123456786")]
    [InlineData("  20-12345678-6  ")]
    public void Create_ValidTaxIdWithOrWithoutSeparators_NormalizesToElevenDigits(string raw)
    {
        Validated<TaxId> result = TaxId.Create(raw);

        Assert.True(result.IsValid);
        Assert.Equal("20123456786", result.Value.Value);
    }

    [Fact]
    public void Create_CompanyTaxId_IsValid()
    {
        Validated<TaxId> result = TaxId.Create("30-71234567-1");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_RemainderZero_RequiresCheckDigitZero()
    {
        Validated<TaxId> result = TaxId.Create("20-30000007-0");

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("20-30000002-0")]
    [InlineData("20-30000002-1")]
    [InlineData("20-30000002-9")]
    public void Create_ComputedCheckDigitIsTen_IsInvalid(string raw)
    {
        Validated<TaxId> result = TaxId.Create(raw);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Create_WrongCheckDigit_ReturnsCheckDigitError()
    {
        Validated<TaxId> result = TaxId.Create("20-12345678-5");

        FieldError error = Assert.Single(result.Errors);
        Assert.Equal(nameof(Customer.TaxId), error.Field);
        Assert.Contains("dígito verificador", error.Message);
    }

    [Theory]
    [InlineData("2012345678")]
    [InlineData("201234567861")]
    [InlineData("20-1234567A-6")]
    [InlineData("20.12345678.6")]
    public void Create_NotElevenDigits_IsInvalid(string raw)
    {
        Validated<TaxId> result = TaxId.Create(raw);

        FieldError error = Assert.Single(result.Errors);
        Assert.Equal(nameof(Customer.TaxId), error.Field);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Missing_ReturnsRequiredError(string? raw)
    {
        Validated<TaxId> result = TaxId.Create(raw);

        FieldError error = Assert.Single(result.Errors);
        Assert.Equal(nameof(Customer.TaxId), error.Field);
        Assert.Contains("obligatorio", error.Message);
    }

    [Fact]
    public void Equals_SameNormalizedValue_AreEqual()
    {
        TaxId withSeparators = TaxId.Create("20-12345678-6").Value;
        TaxId withoutSeparators = TaxId.Create("20123456786").Value;

        Assert.Equal(withSeparators, withoutSeparators);
    }
}
