namespace MiniErp.Domain.Common;

/// <summary>A validation error tied to the field that caused it.</summary>
public sealed record FieldError(string Field, string Message);
