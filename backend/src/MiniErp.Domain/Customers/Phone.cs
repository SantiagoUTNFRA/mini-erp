using MiniErp.Domain.Common;

namespace MiniErp.Domain.Customers;

/// <summary>Contact phone, as written by the user but restricted to phone characters.</summary>
public sealed record Phone
{
    public const int MaxLength = 30;
    private const int MinDigits = 6;

    private Phone(string value) => Value = value;

    public string Value { get; }

    public static Validated<Phone> Create(string raw)
    {
        string value = raw.Trim();

        if (value.Length > MaxLength)
        {
            return Validated<Phone>.Failure(nameof(Customer.Phone), $"El teléfono no puede superar los {MaxLength} caracteres.");
        }

        if (!HasOnlyPhoneCharacters(value) || value.Count(char.IsAsciiDigit) < MinDigits)
        {
            return Validated<Phone>.Failure(
                nameof(Customer.Phone),
                $"El teléfono solo admite dígitos, espacios, guiones, paréntesis y un '+' inicial, con al menos {MinDigits} dígitos.");
        }

        return Validated<Phone>.Success(new Phone(value));
    }

    public override string ToString() => Value;

    private static bool HasOnlyPhoneCharacters(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            bool allowed = char.IsAsciiDigit(c) || c is ' ' or '-' or '(' or ')' || (c == '+' && i == 0);
            if (!allowed)
            {
                return false;
            }
        }

        return true;
    }
}
