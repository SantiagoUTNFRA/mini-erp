using MiniErp.Domain.Customers;

namespace MiniErp.Application.Customers;

/// <summary>A page of customers together with the paging that produced it.</summary>
public sealed record CustomerPageResult(IReadOnlyList<Customer> Items, int Page, int PageSize, int TotalCount);
