namespace Core.Common.Querying.Projection;

/// <summary>One tabular row; values follow the column order.</summary>
public sealed record TabularRow(IReadOnlyList<object?> Values);