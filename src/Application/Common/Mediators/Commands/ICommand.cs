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

/// <summary>
/// Marker interface for commands that own their persistence boundary.
/// </summary>
/// <remarks>
/// The transaction behavior skips these commands, so a slice backed by an external
/// service (for example ASP.NET Core Identity) does not open an ambient unit of work.
/// </remarks>
public interface INonTransactionalCommand;