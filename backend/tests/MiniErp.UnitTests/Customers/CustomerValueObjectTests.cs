using MiniErp.Domain.Common;
using MiniErp.Domain.Customers;

namespace MiniErp.UnitTests.Customers;

public class CustomerValueObjectTests
{
    // LegalName: "Trim; no vacío; ≤ 200 caracteres"

    [Fact]
    public void LegalNameCreate_SurroundingSpaces_AreTrimmed()
    {
        Validated<LegalName> result = LegalName.Create("  Pérez, Juan  ");

        Assert.Equal("Pérez, Juan", result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LegalNameCreate_Missing_ReturnsRequiredError(string? raw)
    {
        Validated<LegalName> result = LegalName.Create(raw);

        FieldError error = Assert.Single(result.Errors);
        Assert.Equal(nameof(Customer.LegalName), error.Field);
    }

    [Fact]
    public void LegalNameCreate_ExactlyMaxLengthAfterTrim_IsValid()
    {
        Validated<LegalName> result = LegalName.Create("  " + new string('a', 200) + "  ");

        Assert.True(result.IsValid);
        Assert.Equal(200, result.Value.Value.Length);
    }

    [Fact]
    public void LegalNameCreate_OverMaxLength_IsRejectedNotTruncated()
    {
        Validated<LegalName> result = LegalName.Create(new string('a', 201));

        Assert.False(result.IsValid);
    }

    // Email: "Trim + minúsculas; un único @, parte local y dominio no vacíos, dominio con al menos un .
    // que no esté al inicio ni al final, y sin espacios; ≤ 254 caracteres"

    [Fact]
    public void EmailCreate_UppercaseAndSpaces_AreNormalized()
    {
        Validated<Email> result = Email.Create("  Juan.Perez@Example.COM ");

        Assert.Equal("juan.perez@example.com", result.Value.Value);
    }

    [Theory]
    [InlineData("juan@example.com")]
    [InlineData("juan+ventas@mail.example.com.ar")]
    public void EmailCreate_ValidFormat_IsValid(string raw)
    {
        Validated<Email> result = Email.Create(raw);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("juanexample.com")]
    [InlineData("juan@@example.com")]
    [InlineData("ju@an@example.com")]
    [InlineData("@example.com")]
    [InlineData("juan@")]
    [InlineData("juan@example")]
    [InlineData("juan@.example.com")]
    [InlineData("juan@example.com.")]
    [InlineData("juan perez@example.com")]
    public void EmailCreate_InvalidFormat_ReturnsEmailError(string raw)
    {
        Validated<Email> result = Email.Create(raw);

        FieldError error = Assert.Single(result.Errors);
        Assert.Equal(nameof(Customer.Email), error.Field);
    }

    [Fact]
    public void EmailCreate_OverMaxLength_IsRejected()
    {
        string tooLong = new string('a', 246) + "@mail.com"; // 254 + 1

        Validated<Email> result = Email.Create(tooLong);

        Assert.Equal(255, tooLong.Length);
        Assert.False(result.IsValid);
    }

    // Phone: "Trim; solo dígitos, espacios, -, (, ) y un + inicial; ≥ 6 dígitos; ≤ 30 caracteres"

    [Theory]
    [InlineData("+54 11 4321-5678")]
    [InlineData("(011) 4321-5678")]
    [InlineData("432156")]
    public void PhoneCreate_AllowedCharacters_IsValid(string raw)
    {
        Validated<Phone> result = Phone.Create(raw);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("4321-5678 int. 12")]
    [InlineData("11+43215678")]
    [InlineData("++5411432156")]
    [InlineData("12345")]
    [InlineData("(12) 3-4")]
    public void PhoneCreate_InvalidCharactersOrTooFewDigits_ReturnsPhoneError(string raw)
    {
        Validated<Phone> result = Phone.Create(raw);

        FieldError error = Assert.Single(result.Errors);
        Assert.Equal(nameof(Customer.Phone), error.Field);
    }

    [Fact]
    public void PhoneCreate_OverMaxLength_IsRejected()
    {
        Validated<Phone> result = Phone.Create(new string('1', 31));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void PhoneCreate_SurroundingSpaces_AreTrimmed()
    {
        Validated<Phone> result = Phone.Create("  4321-5678  ");

        Assert.Equal("4321-5678", result.Value.Value);
    }

    // Address: "Trim; texto libre; ≤ 300 caracteres"

    [Fact]
    public void AddressCreate_FreeText_IsTrimmed()
    {
        Validated<Address> result = Address.Create("  Av. Siempre Viva 742, 3° B  ");

        Assert.Equal("Av. Siempre Viva 742, 3° B", result.Value.Value);
    }

    [Fact]
    public void AddressCreate_OverMaxLength_ReturnsAddressError()
    {
        Validated<Address> result = Address.Create(new string('a', 301));

        FieldError error = Assert.Single(result.Errors);
        Assert.Equal(nameof(Customer.Address), error.Field);
    }
}
