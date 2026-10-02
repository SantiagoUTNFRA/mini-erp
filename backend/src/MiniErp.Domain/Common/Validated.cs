namespace MiniErp.Domain.Common;

/// <summary>Either a valid value or the full list of errors that prevented creating it.</summary>
public sealed class Validated<T>
{
    private readonly T _value;

    private Validated(T value, IReadOnlyList<FieldError> errors)
    {
        _value = value;
        Errors = errors;
    }

    public IReadOnlyList<FieldError> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    public T Value => IsValid
        ? _value
        : throw new InvalidOperationException("Cannot read the value of an invalid result.");

    public static Validated<T> Success(T value) => new(value, []);

    // default! is never observable: Value throws when there are errors.
    public static Validated<T> Failure(IReadOnlyList<FieldError> errors) =>
        errors.Count == 0
            ? throw new ArgumentException("A failure needs at least one error.", nameof(errors))
            : new(default!, errors);

    public static Validated<T> Failure(string field, string message) => Failure([new FieldError(field, message)]);
}
