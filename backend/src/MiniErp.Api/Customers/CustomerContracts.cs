using System.Text.Json;

using MiniErp.Application.Customers;
using MiniErp.Domain.Customers;

namespace MiniErp.Api.Customers;

/// <summary>
/// Body of POST and PUT. Every field arrives as a string and is validated by the domain, so an invalid
/// value is reported together with the other errors instead of failing deserialization.
/// </summary>
public sealed record CustomerRequest(
    string? LegalName,
    string? TaxId,
    string? VatCondition,
    string? Email,
    string? Phone,
    string? Address)
{
    public CustomerData ToData() => new(LegalName, TaxId, VatCondition, Email, Phone, Address);
}

public sealed record CustomerResponse(
    Guid Id,
    string LegalName,
    string TaxId,
    string VatCondition,
    string? Email,
    string? Phone,
    string? Address)
{
    public static CustomerResponse From(Customer customer) => new(
        customer.Id,
        customer.LegalName.Value,
        customer.TaxId.Value,
        JsonNamingPolicy.CamelCase.ConvertName(customer.VatCondition.ToString()),
        customer.Email?.Value,
        customer.Phone?.Value,
        customer.Address?.Value);
}

public sealed record CustomerPageResponse(
    IReadOnlyList<CustomerResponse> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public static CustomerPageResponse From(CustomerPageResult result) => new(
        result.Items.Select(CustomerResponse.From).ToList(),
        result.Page,
        result.PageSize,
        result.TotalCount);
}
