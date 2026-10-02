using MiniErp.Domain.Common;

namespace MiniErp.Application.Common;

public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict,
}

/// <summary>An expected business failure of a use case.</summary>
public sealed record Error(ErrorKind Kind, string Title, IReadOnlyList<FieldError> FieldErrors)
{
    public static Error Validation(IReadOnlyList<FieldError> fieldErrors) =>
        new(ErrorKind.Validation, "One or more validation errors occurred.", fieldErrors);

    public static Error NotFound(string title) => new(ErrorKind.NotFound, title, []);

    public static Error Conflict(string title) => new(ErrorKind.Conflict, title, []);
}

/// <summary>Outcome of a use case that returns nothing on success.</summary>
public sealed class Result
{
    private Result(Error? error) => Error = error;

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Outcome of a use case: a value on success, or the expected <see cref="Common.Error"/>.</summary>
public sealed class Result<T>
{
    private readonly T _value;

    private Result(T value, Error? error)
    {
        _value = value;
        Error = error;
    }

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("Cannot read the value of a failed result.");

    public static Result<T> Success(T value) => new(value, null);

    // default! is never observable: Value throws on failure.
    public static Result<T> Failure(Error error) => new(default!, error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
