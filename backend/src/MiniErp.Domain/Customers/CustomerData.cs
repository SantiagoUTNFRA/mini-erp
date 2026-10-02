namespace MiniErp.Domain.Customers;

/// <summary>Raw customer data as received, before validation. Every field may be missing.</summary>
public sealed record CustomerData(
    string? LegalName,
    string? TaxId,
    string? VatCondition,
    string? Email,
    string? Phone,
    string? Address);
