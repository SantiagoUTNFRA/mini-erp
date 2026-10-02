using MiniErp.Application.Common;
using MiniErp.Domain.Common;

namespace MiniErp.Application.Customers;

/// <summary>Lists customers ordered by legal name, one page at a time (US2, FR-005).</summary>
public sealed class ListCustomers(ICustomerRepository repository)
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public async Task<Result<CustomerPageResult>> ExecuteAsync(int? page, int? pageSize, CancellationToken cancellationToken)
    {
        int requestedPage = page ?? DefaultPage;
        int requestedPageSize = pageSize ?? DefaultPageSize;

        List<FieldError> errors = [];
        if (requestedPage < 1)
        {
            errors.Add(new FieldError("Page", "La página debe ser mayor o igual a 1."));
        }

        if (requestedPageSize is < 1 or > MaxPageSize)
        {
            errors.Add(new FieldError("PageSize", $"El tamaño de página debe estar entre 1 y {MaxPageSize}."));
        }

        if (errors.Count > 0)
        {
            return Error.Validation(errors);
        }

        CustomerPage customerPage = await repository.ListAsync(requestedPage, requestedPageSize, cancellationToken);

        return new CustomerPageResult(customerPage.Items, requestedPage, requestedPageSize, customerPage.TotalCount);
    }
}
