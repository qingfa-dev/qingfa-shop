using System.Data;

using Application.BuildingBlocks.UseCases.Mediation;
using Application.BuildingBlocks.UseCases.Mediation.Commands;
using Application.BuildingBlocks.Persistence.UnitOfWorks;

using MediatR;

namespace Application.BuildingBlocks.UseCases.Mediation.Behaviors.Transactions;

/// <summary>
/// Wraps command handlers in one unit-of-work transaction.
/// </summary>
/// <remarks>
/// Queries pass through untouched. A failed result rolls back, an exception rolls back
/// before it is rethrown, and a success commits. The transaction is always disposed.
/// </remarks>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The result response type.</typeparam>
public sealed class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult<Error>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsCommand(request))
            return await next(cancellationToken);

        Result<ITransaction> begin = await unitOfWork
            .BeginTransactionAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (begin.IsFailure)
            return MediatorResult.Failure<TResponse>(begin.Errors);

        await using ITransaction transaction = begin.Value;

        try
        {
            TResponse response = await next(cancellationToken).ConfigureAwait(false);

            if (response is Result { IsFailure: true })
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return response;
            }

            Result commit = await transaction
                .CommitAsync(cancellationToken)
                .ConfigureAwait(false);

            return commit.IsFailure
                ? MediatorResult.Failure<TResponse>(commit.Errors)
                : response;
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            // Boundary: cancellation and unexpected exceptions must reach the caller/exception behavior.
            throw;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Determines whether the request is a command.</summary>
    /// <param name="request">The request to inspect.</param>
    /// <returns><c>true</c> for commands; otherwise <c>false</c>.</returns>
    private static bool IsCommand(TRequest request)
        => request is not INonTransactionalCommand
            && (request is ICommand
                || typeof(TRequest).GetInterfaces().Any(
                    contract => contract.IsGenericType
                        && contract.GetGenericTypeDefinition() == typeof(ICommandBase<>)));
}
