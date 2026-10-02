using MiniErp.Domain.Common;

namespace MiniErp.Domain.Customers;

/// <summary>Contact email, stored trimmed and in lowercase.</summary>
public sealed record Email
{
    public const int MaxLength = 254;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Validated<Email> Create(string raw)
    {
        string value = raw.Trim().ToLowerInvariant();

        if (value.Length > MaxLength)
        {
            return Validated<Email>.Failure(nameof(Customer.Email), $"El email no puede superar los {MaxLength} caracteres.");
        }

        if (!HasValidFormat(value))
        {
            return Validated<Email>.Failure(nameof(Customer.Email), "El email no tiene un formato válido.");
        }

        return Validated<Email>.Success(new Email(value));
    }

    public override string ToString() => Value;

    // One '@', non-empty local part and domain, a '.' in the domain that is neither first nor last,
    // and no whitespace (spec FR-004).
    private static bool HasValidFormat(string value)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            return false;
        }

        string[] parts = value.Split('@');
        if (parts is not [{ Length: > 0 }, { Length: > 0 } domain])
        {
            return false;
        }

        return domain.Contains('.') && !domain.StartsWith('.') && !domain.EndsWith('.');
    }
}
