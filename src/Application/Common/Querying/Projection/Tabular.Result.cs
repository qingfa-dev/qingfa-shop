namespace Application.Common.Querying.Projection;

/// <summary>The result of a select, group, or aggregate query.</summary>
/// <param name="Columns">Column metadata (name and value type) for each projected field.</param>
/// <param name="Rows">Rows of data, each containing values that correspond to the columns.</param>
public sealed record TabularResult(
    IReadOnlyList<TabularColumn> Columns,
    IReadOnlyList<TabularRow> Rows);