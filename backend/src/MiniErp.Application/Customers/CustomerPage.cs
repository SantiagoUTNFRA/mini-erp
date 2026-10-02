using MiniErp.Domain.Customers;

namespace MiniErp.Application.Customers;

public sealed record CustomerPage(IReadOnlyList<Customer> Items, int TotalCount);
