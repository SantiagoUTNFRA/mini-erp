using System.Text.Json;

using Microsoft.AspNetCore.Http.HttpResults;

using MiniErp.Application.Common;

namespace MiniErp.Api.Customers;

/// <summary>Translates use case errors into HTTP responses (RFC 9457 problem details).</summary>
internal static class ResultMapping
{
    /// <summary>400 with every invalid field, keyed by its name in the request (camelCase).</summary>
    public static ValidationProblem ToValidationProblem(this Error error) =>
        TypedResults.ValidationProblem(
            error.FieldErrors
                .GroupBy(fieldError => JsonNamingPolicy.CamelCase.ConvertName(fieldError.Field))
                .ToDictionary(group => group.Key, group => group.Select(fieldError => fieldError.Message).ToArray()));

    /// <summary>404 or 409 with the error title.</summary>
    public static ProblemHttpResult ToProblem(this Error error) =>
        TypedResults.Problem(
            title: error.Title,
            statusCode: error.Kind switch
            {
                ErrorKind.NotFound => StatusCodes.Status404NotFound,
                ErrorKind.Conflict => StatusCodes.Status409Conflict,
                _ => throw new ArgumentOutOfRangeException(nameof(error), error.Kind, "Validation errors map to ValidationProblem."),
            });
}
