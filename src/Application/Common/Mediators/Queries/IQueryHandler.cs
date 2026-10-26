using MediatR;

namespace Application.Common.Mediators.Queries;

/// <summary>
/// Contract for handling queries that return a typed result response.
/// </summary>
/// <typeparam name="TQuery">The type of the query being handled.</typeparam>
/// <typeparam name="TResponse">The type of the data returned on success.</typeparam>
public interface IQueryHandler<TQuery, TResponse>
    : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;