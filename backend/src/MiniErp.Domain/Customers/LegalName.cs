using MiniErp.Domain.Common;

namespace MiniErp.Domain.Customers;

/// <summary>Razón social; for a natural person, the full name.</summary>
public sealed record LegalName
{
    public const int MaxLength = 200;

    private LegalName(string value) => Value = value;

    public string Value { get; }

    public static Validated<LegalName> Create(string? raw)
    {
        string value = raw?.Trim() ?? string.Empty;

        if (value.Length == 0)
        {
            return Validated<LegalName>.Failure(nameof(Customer.LegalName), "La razón social es obligatoria.");
        }

        if (value.Length > MaxLength)
        {
            return Validated<LegalName>.Failure(nameof(Customer.LegalName), $"La razón social no puede superar los {MaxLength} caracteres.");
        }

        return Validated<LegalName>.Success(new LegalName(value));
    }

    public override string ToString() => Value;
}
