using System.Linq.Expressions;
using System.Reflection;

using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Common.Querying.Sorting;

namespace Infrastructure.Persistence.EntityFramework.QueryTranslation;

public sealed partial class QueryTranslationEfCore<T>
{
    /// <summary>
    /// Applies an ordering to a source query.
    /// Appends a stable ID order at the end so rows with equal sort values page deterministically.
    /// </summary>
    public Result<IQueryable<T>> ApplySort(
        IQueryable<T> source,
        IReadOnlyList<SortField> order,
        QueryMap<T> map)
    {
        // Default: no sort fields means apply the default ID order.
        if (order.Count == 0)
            return Result<IQueryable<T>>.Success(ApplyDefaultOrder(source));

        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        IQueryable<T> query = source;
        bool then = false;

        foreach (SortField sort in order)
        {
            // Resolve: look up the field and check sortability.
            Result<QueryField> field = Resolve(map, sort.Field, sorting: true);
            if (field.IsFailure)
                return Result<IQueryable<T>>.Fail(field.Errors);

            Result<Expression> member = BuildMember(field.Value, parameter, indexValue: null);
            if (member.IsFailure)
                return Result<IQueryable<T>>.Fail(member.Errors);

            // Guard: wrap the key so null intermediates produce default(T) instead of throwing.
            Expression key = BuildSafeKey(member.Value);

            // Validate: null-state ordering only applies to nullable members.
            if (sort.Nulls != SortNulls.None && !IsNullable(member.Value.Type))
            {
                return Result<IQueryable<T>>.Fail(
                    QueryOutcome.Failure.OrderSyntax($"field '{sort.Field}' is not nullable"));
            }

            // Sort: order by the requested null state first; `field?` (HasValue) puts
            // non-nulls first, `field!` (NotHasValue) puts nulls first.
            if (sort.Nulls != SortNulls.None)
            {
                Expression nullState = BuildNullState(member.Value, sort.Nulls);

                // Guard: a missing navigation means the leaf is null; fold that into the
                // null-state key instead of reading it through the safe default value.
                Expression? guard = BuildGuard(member.Value, includeLeaf: false);
                if (guard is not null)
                {
                    nullState = sort.Nulls == SortNulls.HasValue
                        ? Expression.OrElse(Expression.Not(guard), nullState)
                        : Expression.AndAlso(guard, nullState);
                }

                query = ApplyOrder(
                    query,
                    Expression.Lambda(nullState, parameter),
                    SortDirection.Ascending,
                    then);
                then = true;
            }

            query = ApplyOrder(
                query, Expression.Lambda(key, parameter), sort.Direction, then);
            then = true;
        }

        // Stabilize: end on the key so rows with equal sort values page deterministically.
        return Result<IQueryable<T>>.Success(ApplyIdOrder(query, then: true));
    }

    /// <summary>Applies a page window to a source query.</summary>
    public Result<IQueryable<T>> ApplyPaging(IQueryable<T> source, PageRequest page)
    {
        // Validate: a non-positive window or an offset beyond int range would make Skip throw.
        if (page.Page < 1 || page.PageSize < 1 ||
            (long)(page.Page - 1) * page.PageSize > int.MaxValue)
        {
            return Result<IQueryable<T>>.Fail(
                QueryOutcome.Failure.Syntax("page window is out of range"));
        }

        // Compute: convert 1-based page to 0-based offset for Skip.
        return Result<IQueryable<T>>.Success(
            source.Skip((page.Page - 1) * page.PageSize).Take(page.PageSize));
    }

    /// <summary>
    /// Builds a null-state boolean expression for nulls-first or nulls-last ordering.
    /// Uses HasValue for Nullable&lt;T&gt; and a null comparison for reference types.
    /// </summary>
    private static Expression BuildNullState(Expression member, SortNulls nulls)
    {
        // Validate: Nullable<T> exposes HasValue; reference types use a null comparison.
        Expression hasValue = Nullable.GetUnderlyingType(member.Type) is not null
            ? Expression.Property(member, nameof(Nullable<int>.HasValue))
            : Expression.NotEqual(member, Expression.Constant(null, member.Type));

        // Invert: HasValue means non-null first, so the ascending key is !HasValue;
        // NotHasValue orders nulls first, so the ascending key is HasValue.
        return nulls == SortNulls.HasValue ? Expression.Not(hasValue) : hasValue;
    }

    /// <summary>
    /// Wraps a sort key with a null guard so intermediate navigation access does not throw
    /// NullReferenceException when a reference property is null.
    /// </summary>
    private static Expression BuildSafeKey(Expression member)
    {
        // Guard: a null intermediate must not throw; fall back to default(T) for the key.
        Expression? guard = BuildGuard(member, includeLeaf: false);
        return guard is null
            ? member
            // Conditional: if the guard passes, use the member; otherwise use default(T).
            : Expression.Condition(guard, member, Expression.Default(member.Type));
    }

    /// <summary>
    /// Applies a single order-by clause using the correct Queryable method
    /// (OrderBy, OrderByDescending, ThenBy, ThenByDescending).
    /// </summary>
    private static IQueryable<T> ApplyOrder(
        IQueryable<T> source,
        LambdaExpression key,
        SortDirection direction,
        bool then)
    {
        // Select: choose the correct Queryable method based on direction and position.
        string method = then
            ? direction == SortDirection.Ascending ? nameof(Queryable.ThenBy) : nameof(Queryable.ThenByDescending)
            : direction == SortDirection.Ascending ? nameof(Queryable.OrderBy) : nameof(Queryable.OrderByDescending);

        MethodCallExpression call = Expression.Call(
            typeof(Queryable),
            method,
            [typeof(T), key.ReturnType],
            source.Expression,
            Expression.Quote(key));
        return source.Provider.CreateQuery<T>(call);
    }

    private static readonly PropertyInfo? IdProperty =
        typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);

    /// <summary>
    /// Applies a default order by the entity's Id property ascending.
    /// Used when no explicit order-by is provided.
    /// </summary>
    internal static IQueryable<T> ApplyDefaultOrder(IQueryable<T> source) =>
        ApplyIdOrder(source, then: false);

    /// <summary>
    /// Applies an order-by on the entity's Id property for deterministic paging.
    /// Skipped if the entity type has no Id property.
    /// </summary>
    private static IQueryable<T> ApplyIdOrder(IQueryable<T> source, bool then)
    {
        // Skip: not all entities have an Id property.
        if (IdProperty is null)
            return source;

        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        return ApplyOrder(
            source,
            Expression.Lambda(Expression.Property(parameter, IdProperty), parameter),
            SortDirection.Ascending,
            then);
    }
}