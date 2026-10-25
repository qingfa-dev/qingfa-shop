namespace Core.Common.Querying.Projection;

/// <summary>Column metadata for a tabular query result.</summary>
public sealed record TabularColumn(string Name, Type ValueType);