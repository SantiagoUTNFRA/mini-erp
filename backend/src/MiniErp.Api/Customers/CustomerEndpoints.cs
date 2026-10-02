using Microsoft.AspNetCore.Http.HttpResults;

using MiniErp.Application.Common;
using MiniErp.Application.Customers;
using MiniErp.Domain.Customers;

namespace MiniErp.Api.Customers;

internal static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomers(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/customers")
            .WithTags("Customers");

        group.MapPost("/", CreateAsync)
            .WithName("CreateCustomer");

        return endpoints;
    }

    private static async Task<Results<Created<CustomerResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        CustomerRequest request,
        CreateCustomer createCustomer,
        CancellationToken cancellationToken)
    {
        Result<Customer> result = await createCustomer.ExecuteAsync(request.ToData(), cancellationToken);

        if (result.Error is { } error)
        {
            return error.Kind == ErrorKind.Validation ? error.ToValidationProblem() : error.ToProblem();
        }

        CustomerResponse response = CustomerResponse.From(result.Value);
        return TypedResults.Created($"/api/customers/{response.Id}", response);
    }
}
