namespace Application.BuildingBlocks.Querying.Projection;

/// <summary>Column metadata for a tabular query result.</summary>
/// <param name="Name">The column name (alias or field name).</param>
/// <param name="ValueType">The .NET type of the column values.</param>
public sealed record TabularColumn(
    string Name,
    Type ValueType);