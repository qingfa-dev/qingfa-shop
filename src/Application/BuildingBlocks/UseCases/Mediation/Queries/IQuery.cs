using MediatR;

namespace Application.BuildingBlocks.UseCases.Mediation.Queries;

/// <summary>
/// Marker interface for queries that return a typed result response.
/// </summary>
/// <typeparam name="TResponse">The type of data returned on success.</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;