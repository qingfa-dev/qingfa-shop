using System.Linq.Expressions;
using System.Reflection;

using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Common.Querying.Sorting;

namespace Infrastructure.Persistence.EntityFramework.QueryTranslation;

public sealed partial class QueryTranslationEfCore<T>
{
    /// <summary>Applies an ordering to a source query.</summary>
    public Result<IQueryable<T>> ApplySort(
        IQueryable<T> source,
        IReadOnlyList<SortField> order,
        QueryMap<T> map)
    {
        if (order.Count == 0)
            return Result<IQueryable<T>>.Success(ApplyDefaultOrder(source));

        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        IQueryable<T> query = source;
        bool then = false;

        foreach (SortField sort in order)
        {
            Result<QueryField> field = Resolve(map, sort.Field, sorting: true);
            if (field.IsFailure)
                return Result<IQueryable<T>>.Fail(field.Errors);

            Result<Expression> member = BuildMember(field.Value, parameter, indexValue: null);
            if (member.IsFailure)
                return Result<IQueryable<T>>.Fail(member.Errors);

            Expression key = BuildSafeKey(member.Value);

            // Validate: null-state ordering only applies to nullable members.
            if (sort.Nulls != SortNulls.None && !IsNullable(member.Value.Type))
            {
                return Result<IQueryable<T>>.Fail(
                    QueryOutcome.Failure.OrderSyntax($"field '{sort.Field}' is not nullable"));
            }

            // Sort: order by the requested null state first; `field?` puts nulls first,
            // `field!` puts them last, matching the parser's SortNulls contract.
            if (sort.Nulls != SortNulls.None)
            {
                query = ApplyOrder(
                    query,
                    Expression.Lambda(BuildNullState(key, sort.Nulls), parameter),
                    SortDirection.Ascending,
                    then);
                then = true;
            }

            query = ApplyOrder(
                query, Expression.Lambda(key, parameter), sort.Direction, then);
            then = true;
        }

        // Sort: end on the key so rows with equal sort values page deterministically.
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

        return Result<IQueryable<T>>.Success(
            source.Skip((page.Page - 1) * page.PageSize).Take(page.PageSize));
    }

    private static Expression BuildNullState(Expression member, SortNulls nulls)
    {
        // Validate: Nullable<T> exposes HasValue; reference types use a null comparison.
        Expression hasValue = Nullable.GetUnderlyingType(member.Type) is not null
            ? Expression.Property(member, nameof(Nullable<int>.HasValue))
            : Expression.NotEqual(member, Expression.Constant(null, member.Type));

        return nulls == SortNulls.HasValue ? hasValue : Expression.Not(hasValue);
    }

    private static Expression BuildSafeKey(Expression member)
    {
        // Sort: a null intermediate must not throw; fall back to default(T) for the key.
        Expression? guard = BuildGuard(member, includeLeaf: false);
        return guard is null
            ? member
            : Expression.Condition(guard, member, Expression.Default(member.Type));
    }

    private static IQueryable<T> ApplyOrder(
        IQueryable<T> source,
        LambdaExpression key,
        SortDirection direction,
        bool then)
    {
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

    internal static IQueryable<T> ApplyDefaultOrder(IQueryable<T> source) =>
        ApplyIdOrder(source, then: false);

    private static IQueryable<T> ApplyIdOrder(IQueryable<T> source, bool then)
    {
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