namespace MiniErp.Application.Customers;

/// <summary>Titles of the customer business errors. Part of the HTTP contract (contracts/customers-api.md).</summary>
public static class CustomerErrors
{
    public const string AlreadyExistsTitle = "Customer already exists";
    public const string NotFoundTitle = "Customer not found";
    public const string HasInvoicesTitle = "Customer has invoices";
}
