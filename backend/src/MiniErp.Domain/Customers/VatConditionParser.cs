using MiniErp.Domain.Common;

namespace MiniErp.Domain.Customers;

public static class VatConditionParser
{
    /// <summary>Parses a <see cref="VatCondition"/> by name, ignoring case and surrounding spaces.</summary>
    public static Validated<VatCondition> Parse(string? raw)
    {
        string value = raw?.Trim() ?? string.Empty;

        if (value.Length == 0)
        {
            return Validated<VatCondition>.Failure(nameof(Customer.VatCondition), "La condición frente al IVA es obligatoria.");
        }

        // Only letters: Enum.TryParse would also accept numbers ("1") and combinations ("a,b").
        if (value.All(char.IsAsciiLetter)
            && Enum.TryParse(value, ignoreCase: true, out VatCondition condition)
            && Enum.IsDefined(condition))
        {
            return Validated<VatCondition>.Success(condition);
        }

        return Validated<VatCondition>.Failure(nameof(Customer.VatCondition), "La condición frente al IVA no es válida.");
    }
}
