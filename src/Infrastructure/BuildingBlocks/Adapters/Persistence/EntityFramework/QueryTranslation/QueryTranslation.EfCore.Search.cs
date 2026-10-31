using System.Linq.Expressions;

using Application.BuildingBlocks.Querying.Mapping;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

public sealed partial class QueryTranslationEfCore<T>
{
    /// <summary>
    /// Applies a free-text search across every searchable field.
    /// Only string fields are searched — non-string fields cannot contain text.
    /// Fields are combined with OR logic so a match in any searchable field satisfies the search.
    /// </summary>
    public Result<IQueryable<T>> ApplySearch(IQueryable<T> source, string? search, QueryMap<T> map)
    {
        // Skip: no search text means no filter.
        if (string.IsNullOrWhiteSpace(search))
            return Result<IQueryable<T>>.Success(source);

        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        Expression? body = null;

        // Filter: only single string-valued members can take a text search; composite,
        // collection, indexed, and non-string fields are skipped instead of failing the query.
        foreach (QueryField field in map.Fields.Where(IsTextSearchable))
        {
            Result<Expression> member = BuildMember(field, parameter, indexValue: null);
            if (member.IsFailure)
                return Result<IQueryable<T>>.Fail(member.Errors);

            Expression? guard = BuildGuard(member.Value, includeLeaf: true);
            // Normalize: both sides use ToLower for case-insensitive substring matching.
            Expression lower = Expression.Call(member.Value, nameof(string.ToLower), Type.EmptyTypes);
            Expression value = Parameterize(search.Trim().ToLowerInvariant(), typeof(string));
            Expression contains = Expression.Call(lower, nameof(string.Contains), Type.EmptyTypes, value);
            if (guard is not null)
                contains = Expression.AndAlso(guard, contains);

            // Combine: OR each field so a match in any searchable field satisfies the search.
            body = body is null ? contains : Expression.OrElse(body, contains);
        }

        return body is null
            ? Result<IQueryable<T>>.Success(source)
            : Result<IQueryable<T>>.Success(
                source.Where(Expression.Lambda<Func<T, bool>>(body, parameter)));
    }
}