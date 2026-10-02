using MiniErp.Domain.Customers;

namespace MiniErp.IntegrationTests.Infrastructure;

/// <summary>Builds valid customers for seeding tests.</summary>
public static class TestCustomers
{
    private static readonly int[] Weights = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];

    public static Customer Create(string legalName, string? taxId = null, int index = 0) =>
        Customer.Create(new CustomerData(
            LegalName: legalName,
            TaxId: taxId ?? ValidTaxId(index),
            VatCondition: "finalConsumer",
            Email: null,
            Phone: null,
            Address: null)).Value;

    /// <summary>
    /// A distinct valid CUIT for each index. Computed here on purpose instead of reusing the domain,
    /// so a bug in the domain algorithm cannot hide in the test data.
    /// </summary>
    public static string ValidTaxId(int index)
    {
        // Each index owns a block of 10 candidates. Consecutive bodies change the remainder by 2
        // (mod 11), so at most one in a block has a computed check digit of 10 (never valid).
        for (long body = 3_000_000_000 + (index * 10L); ; body++)
        {
            string digits = body.ToString(System.Globalization.CultureInfo.InvariantCulture);
            int sum = 0;
            for (int i = 0; i < Weights.Length; i++)
            {
                sum += (digits[i] - '0') * Weights[i];
            }

            int remainder = sum % 11;
            int checkDigit = remainder == 0 ? 0 : 11 - remainder;
            if (checkDigit != 10)
            {
                return digits + checkDigit;
            }
        }
    }
}
