namespace Application.Common.Querying;


/// <summary>Cross-cutting options that shape how the adapter executes a query.</summary>
public sealed record QueryExecutionOptions
{
    public static QueryExecutionOptions Default { get; } = new();

    /// <summary>Include soft-deleted rows. Adapter must honour it.</summary>
    public bool IncludeSoftDeleted { get; init; }

    /// <summary>DISTINCT over the projection.</summary>
    public bool Distinct { get; init; }

    /// <summary>EF Core: <c>AsNoTracking</c>. Ignored by Dapper.</summary>
    public bool AsNoTracking { get; init; } = true;

    /// <summary>EF Core: <c>AsSplitQuery</c>. Ignored by Dapper.</summary>
    public bool AsSplitQuery { get; init; }

    /// <summary>Navigation paths for eager loading. Empty when the adapter ignores includes.</summary>
    public IReadOnlyList<string> Includes { get; init; } = Array.Empty<string>();

    /// <summary>Hard row cap. Adapters must honour it even when <see cref="Query{T}.Page"/> is null.</summary>
    public int? MaxRows { get; init; }

    /// <summary>Command timeout. Null = provider default.</summary>
    public TimeSpan? CommandTimeout { get; init; }
}