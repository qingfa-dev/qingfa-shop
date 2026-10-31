namespace Core.BuildingBlocks.Domain.Specifications;

/// <summary>Queryable helpers for specifications.</summary>
public static class SpecificationExtensions
{
    /// <summary>Filters a query with a specification.</summary>
    public static IQueryable<T> Where<T>(
        this IQueryable<T> query,
        ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);

        return query.Where(specification.ToExpression());
    }

    /// <summary>Returns true when any row matches the specification.</summary>
    public static bool Any<T>(
        this IQueryable<T> query,
        ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);

        return query.Any(specification.ToExpression());
    }
}