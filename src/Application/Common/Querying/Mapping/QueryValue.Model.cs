namespace Application.Common.Querying.Mapping;

/// <summary>
/// A converted query value. The wrapper makes a successful <c>null</c>
/// representable, which <see cref="Result{TValue}"/> cannot do.
/// </summary>
public readonly record struct QueryValue(object? Value);