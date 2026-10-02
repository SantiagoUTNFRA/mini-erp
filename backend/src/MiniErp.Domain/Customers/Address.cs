using MiniErp.Domain.Common;

namespace MiniErp.Domain.Customers;

/// <summary>Postal address as free text.</summary>
public sealed record Address
{
    public const int MaxLength = 300;

    private Address(string value) => Value = value;

    public string Value { get; }

    public static Validated<Address> Create(string raw)
    {
        string value = raw.Trim();

        if (value.Length > MaxLength)
        {
            return Validated<Address>.Failure(nameof(Customer.Address), $"La dirección no puede superar los {MaxLength} caracteres.");
        }

        return Validated<Address>.Success(new Address(value));
    }

    public override string ToString() => Value;
}
