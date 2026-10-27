using Application.Common.Querying.Projection;

namespace Infrastructure.Persistence.EntityFramework.QueryTranslation;

/// <summary>A server-side projection plus its column metadata.</summary>
/// <param name="Rows">The queryable that produces rows as object[] arrays, one element per column.</param>
/// <param name="Columns">Column names and value types that describe the shape of each row in Rows.</param>
public sealed record TabularPlan(
    IQueryable<object[]> Rows,
    IReadOnlyList<TabularColumn> Columns);

/// <summary>Holds a filter operand so EF Core binds it as a query parameter, not a literal.</summary>
/// <typeparam name="TValue">The operand type.</typeparam>
internal sealed class QueryValue<TValue>(TValue value)
{
    /// <summary>The operand value that EF Core binds as a parameter for query plan caching.</summary>
    public TValue Value { get; } = value;
}
 