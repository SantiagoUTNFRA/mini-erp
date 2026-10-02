using MiniErp.Domain.Common;

namespace MiniErp.Domain.Customers;

/// <summary>CUIT/CUIL: 11 digits with a valid check digit, stored without separators.</summary>
public sealed record TaxId
{
    private const int Length = 11;
    private static readonly int[] Weights = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];

    private TaxId(string value) => Value = value;

    public string Value { get; }

    public static Validated<TaxId> Create(string? raw)
    {
        string digits = string.Concat((raw ?? string.Empty).Where(c => c is not ('-' or ' ')));

        if (digits.Length == 0)
        {
            return Validated<TaxId>.Failure(nameof(Customer.TaxId), "El CUIT/CUIL es obligatorio.");
        }

        if (digits.Length != Length || !digits.All(char.IsAsciiDigit))
        {
            return Validated<TaxId>.Failure(nameof(Customer.TaxId), "El CUIT/CUIL debe tener 11 dígitos.");
        }

        if (!HasValidCheckDigit(digits))
        {
            return Validated<TaxId>.Failure(nameof(Customer.TaxId), "El CUIT/CUIL no es válido: dígito verificador incorrecto.");
        }

        return Validated<TaxId>.Success(new TaxId(digits));
    }

    public override string ToString() => Value;

    // Modulo 11 with the official weights. A computed check digit of 10 is never valid.
    private static bool HasValidCheckDigit(string digits)
    {
        int sum = 0;
        for (int i = 0; i < Weights.Length; i++)
        {
            sum += (digits[i] - '0') * Weights[i];
        }

        int remainder = sum % 11;
        int expected = remainder == 0 ? 0 : 11 - remainder;

        return expected != 10 && digits[Length - 1] - '0' == expected;
    }
}
