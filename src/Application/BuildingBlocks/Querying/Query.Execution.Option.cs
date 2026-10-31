namespace Application.BuildingBlocks.Querying;


/// <summary>Cross-cutting options that shape how the adapter executes a query.</summary>
public sealed record QueryExecutionOptions
{
    /// <summary>The default options with all flags at their safe defaults (AsNoTracking enabled, no includes).</summary>
    public static QueryExecutionOptions Default { get; } = new();

    /// <summary>Include soft-deleted rows. Adapter must honour it.</summary>
    public bool IncludeSoftDeleted { get; init; }

    /// <summary>Apply DISTINCT to the result set to eliminate duplicate rows in the projection.</summary>
    public bool Distinct { get; init; }

    /// <summary>EF Core: <c>AsNoTracking</c>. Ignored by Dapper. Enabled by default for read-only queries.</summary>
    public bool AsNoTracking { get; init; } = true;

    /// <summary>EF Core: <c>AsSplitQuery</c>. Executes navigation properties as separate SQL queries instead of a single JOIN. Ignored by Dapper.</summary>
    public bool AsSplitQuery { get; init; }

    /// <summary>Navigation paths for eager loading (e.g., "Orders", "Customer.Address"). Empty when the adapter ignores includes.</summary>
    public IReadOnlyList<string> Includes { get; init; } = Array.Empty<string>();

    /// <summary>Hard row cap that limits the maximum rows returned. Adapters must honour it even when <see cref="Query{T}.Page"/> is null.</summary>
    public int? MaxRows { get; init; }

    /// <summary>Command timeout in seconds. Null means the provider default timeout applies.</summary>
    public TimeSpan? CommandTimeout { get; init; }
}