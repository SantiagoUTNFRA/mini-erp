namespace MiniErp.Application.Customers;

/// <summary>
/// Thrown by persistence adapters when storage rejects a duplicate tax id, e.g. when two concurrent
/// requests pass the existence check. Keeps provider-specific errors out of the core.
/// </summary>
public sealed class DuplicateTaxIdException : Exception
{
    public DuplicateTaxIdException()
    {
    }

    public DuplicateTaxIdException(string message)
        : base(message)
    {
    }

    public DuplicateTaxIdException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
