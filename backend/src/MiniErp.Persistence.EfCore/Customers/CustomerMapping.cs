using MiniErp.Domain.Common;
using MiniErp.Domain.Customers;

namespace MiniErp.Persistence.EfCore.Customers;

internal static class CustomerMapping
{
    public static CustomerRecord ToRecord(Customer customer)
    {
        CustomerRecord record = new()
        {
            Id = customer.Id,
            LegalName = string.Empty,
            TaxId = string.Empty,
            VatCondition = string.Empty,
        };
        CopyTo(customer, record);
        return record;
    }

    public static void CopyTo(Customer customer, CustomerRecord record)
    {
        record.LegalName = customer.LegalName.Value;
        record.TaxId = customer.TaxId.Value;
        record.VatCondition = customer.VatCondition.ToString();
        record.Email = customer.Email?.Value;
        record.Phone = customer.Phone?.Value;
        record.Address = customer.Address?.Value;
    }

    /// <summary>
    /// Rebuilds the domain customer through the same factories that validate input. A stored record
    /// that fails them means corrupted data, not a user error.
    /// </summary>
    public static Customer ToDomain(CustomerRecord record) => Customer.Restore(
        record.Id,
        Require(LegalName.Create(record.LegalName), record),
        Require(TaxId.Create(record.TaxId), record),
        Enum.TryParse(record.VatCondition, out VatCondition vatCondition) && Enum.IsDefined(vatCondition)
            ? vatCondition
            : throw Corrupted(record, nameof(record.VatCondition)),
        record.Email is null ? null : Require(Email.Create(record.Email), record),
        record.Phone is null ? null : Require(Phone.Create(record.Phone), record),
        record.Address is null ? null : Require(Address.Create(record.Address), record));

    private static T Require<T>(Validated<T> value, CustomerRecord record) =>
        value.IsValid ? value.Value : throw Corrupted(record, value.Errors[0].Field);

    private static InvalidOperationException Corrupted(CustomerRecord record, string field) =>
        new($"Stored customer {record.Id} has an invalid {field}.");
}
