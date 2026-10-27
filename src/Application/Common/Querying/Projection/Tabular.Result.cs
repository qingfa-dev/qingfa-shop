namespace Application.Common.Querying.Projection;

/// <summary>The result of a select, group, or aggregate query.</summary>
public sealed record TabularResult(
    IReadOnlyList<TabularColumn> Columns,
    IReadOnlyList<TabularRow> Rows);