using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Application.Common.Mediators.Behaviors.Exceptions;

/// <summary>
/// Pipeline behavior that catches unhandled exceptions and converts them to Result failures.
/// Prevents exception leakage to the API layer by mapping common exceptions to appropriate Failure codes.
/// </summary>
/// <typeparam name="TRequest">The type of the request being handled.</typeparam>
/// <typeparam name="TResponse">The type of the response (must inherit from Result).</typeparam>
public sealed partial class ExceptionMappingBehavior<TRequest, TResponse>(
    ILogger<ExceptionMappingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult<Error>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancellation: propagate it; a cancelled request is not a failure result.
            throw;
        }
        catch (Exception ex)
        {
            var requestType = typeof(TRequest).Name;
            var description = $"An unhandled exception occurred while processing {requestType}.";
            if (logger.IsEnabled(LogLevel.Error))
            {
                Logger.UnhandledException(logger, typeof(TRequest).Name, ex);
            }

            Error failure = Error
                .FromException(
                    exception: ex,
                    code: $"{requestType}.Unexpected")
                .WithMessage(description);

            return MediatorResult.Failure<TResponse>([failure]);
        }
    }
}