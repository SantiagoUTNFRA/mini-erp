using MiniErp.Domain.Common;

namespace MiniErp.Domain.Customers;

/// <summary>A person or company that will be invoiced.</summary>
public sealed class Customer
{
    private Customer(Guid id, CustomerValues values)
    {
        Id = id;
        LegalName = values.LegalName;
        TaxId = values.TaxId;
        VatCondition = values.VatCondition;
        Email = values.Email;
        Phone = values.Phone;
        Address = values.Address;
    }

    public Guid Id { get; }

    public LegalName LegalName { get; private set; }

    public TaxId TaxId { get; private set; }

    public VatCondition VatCondition { get; private set; }

    public Email? Email { get; private set; }

    public Phone? Phone { get; private set; }

    public Address? Address { get; private set; }

    /// <summary>Validates every field and creates the customer, or returns all the errors together.</summary>
    public static Validated<Customer> Create(CustomerData data)
    {
        Validated<CustomerValues> values = Validate(data);

        return values.IsValid
            ? Validated<Customer>.Success(new Customer(Guid.CreateVersion7(), values.Value))
            : Validated<Customer>.Failure(values.Errors);
    }

    /// <summary>Rebuilds a customer from values that were already validated (e.g. when loading it).</summary>
    public static Customer Restore(
        Guid id,
        LegalName legalName,
        TaxId taxId,
        VatCondition vatCondition,
        Email? email,
        Phone? phone,
        Address? address) =>
        new(id, new CustomerValues(legalName, taxId, vatCondition, email, phone, address));

    /// <summary>
    /// Replaces all the data: an optional field that is not sent becomes absent. Nothing changes if
    /// any field is invalid.
    /// </summary>
    public Validated<Customer> Update(CustomerData data)
    {
        Validated<CustomerValues> values = Validate(data);
        if (!values.IsValid)
        {
            return Validated<Customer>.Failure(values.Errors);
        }

        (LegalName, TaxId, VatCondition, Email, Phone, Address) = values.Value;

        return Validated<Customer>.Success(this);
    }

    private static Validated<CustomerValues> Validate(CustomerData data)
    {
        Validated<LegalName> legalName = LegalName.Create(data.LegalName);
        Validated<TaxId> taxId = TaxId.Create(data.TaxId);
        Validated<VatCondition> vatCondition = VatConditionParser.Parse(data.VatCondition);
        Validated<Email?> email = Optional(data.Email, Email.Create);
        Validated<Phone?> phone = Optional(data.Phone, Phone.Create);
        Validated<Address?> address = Optional(data.Address, Address.Create);

        FieldError[] errors =
        [
            .. legalName.Errors,
            .. taxId.Errors,
            .. vatCondition.Errors,
            .. email.Errors,
            .. phone.Errors,
            .. address.Errors,
        ];

        return errors.Length > 0
            ? Validated<CustomerValues>.Failure(errors)
            : Validated<CustomerValues>.Success(new CustomerValues(
                legalName.Value, taxId.Value, vatCondition.Value, email.Value, phone.Value, address.Value));
    }

    // Blank or whitespace-only optional fields are absent, not empty.
    private static Validated<T?> Optional<T>(string? raw, Func<string, Validated<T>> create)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Validated<T?>.Success(null);
        }

        Validated<T> result = create(raw);

        return result.IsValid ? Validated<T?>.Success(result.Value) : Validated<T?>.Failure(result.Errors);
    }

    private sealed record CustomerValues(
        LegalName LegalName,
        TaxId TaxId,
        VatCondition VatCondition,
        Email? Email,
        Phone? Phone,
        Address? Address);
}
