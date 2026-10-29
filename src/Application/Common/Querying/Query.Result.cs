using System.Linq.Expressions;

namespace Application.Common.Querying;

/// <summary>Types a query by the response shape it is intended to produce.</summary>
/// <typeparam name="T">The source entity type.</typeparam>
/// <typeparam name="TResult">The intended result shape.</typeparam>
public record Query<T, TResult>(Query<T> Source);

/// <summary>A query that carries an entity-to-response projection.</summary>
/// <typeparam name="T">The source entity type.</typeparam>
/// <typeparam name="TResponse">The projected response type.</typeparam>
public sealed record ProjectedQuery<T, TResponse>(
    Query<T> Source,
    Expression<Func<T, TResponse>> Projection)
    : Query<T, TResponse>(Source);
