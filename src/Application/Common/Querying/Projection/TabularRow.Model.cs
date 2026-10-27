namespace Application.Common.Querying.Projection;

/// <summary>One tabular row; values follow the column order.</summary>
/// <param name="Values">Ordered values that correspond to the columns in the result set.</param>
public sealed record TabularRow(
    IReadOnlyList<object?> Values);