using MediatR;

namespace Application.Common.Mediators.Commands;

/// <summary>
/// Base interface for commands that return a typed result response.
/// </summary>
/// <typeparam name="TResponse">The type of data returned on success.</typeparam>
public interface ICommandBase<TResponse> : IRequest<Result<TResponse>>;

/// <summary>
/// Marker interface for commands that do not return a specific response value (Result only).
/// </summary>
public interface ICommand : IRequest<Result>;

/// <summary>
/// Marker interface for commands that return a typed response.
/// </summary>
/// <typeparam name="TResponse">The type of data returned on success.</typeparam>
public interface ICommand<TResponse> : ICommandBase<TResponse>;