using Application.Common.Querying.Projection;

namespace Infrastructure.Persistence.EntityFramework.QueryTranslation;

/// <summary>A server-side projection plus its column metadata.</summary>
public sealed record TabularPlan(
    IQueryable<object[]> Rows,
    IReadOnlyList<TabularColumn> Columns);

/// <summary>Holds a filter operand so EF Core binds it as a query parameter, not a literal.</summary>
/// <typeparam name="TValue">The operand type.</typeparam>
internal sealed class QueryValue<TValue>(TValue value)
{
    public TValue Value { get; } = value;
}
 