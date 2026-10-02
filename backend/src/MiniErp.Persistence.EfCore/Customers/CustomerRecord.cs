namespace MiniErp.Persistence.EfCore.Customers;

/// <summary>Persistence model of a customer. Mapped to and from the domain by <see cref="CustomerMapping"/>.</summary>
public sealed class CustomerRecord
{
    public Guid Id { get; set; }

    public required string LegalName { get; set; }

    public required string TaxId { get; set; }

    public required string VatCondition { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }
}
