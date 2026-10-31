using Application.BuildingBlocks.Messaging.Bus;

using Wolverine;

namespace Infrastructure.BuildingBlocks.Adapters.Messaging;

/// <summary>Adapts Wolverine's <see cref="IMessageBus"/> to the transport-neutral event bus port.</summary>
/// <remarks>
/// The adapter only hands the message to the transport. The outbox dispatcher marks the row
/// processed after <see cref="PublishAsync{T}"/> succeeds, so delivery stays at-least-once and
/// Wolverine must not create a second outbox.
/// </remarks>
public sealed class WolverineEventBus(IMessageBus bus) : IEventBus
{
    /// <inheritdoc />
    /// <exception cref="OperationCanceledException">
    /// The caller cancelled before the message reached the transport.
    /// </exception>
    public async Task<Result> PublishAsync<T>(
        T integrationEvent,
        CancellationToken cancellationToken = default)
        where T : class, IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        // Guard: Wolverine cannot observe our token, so a cancelled publish must not be sent.
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Send: the dispatcher only retires the outbox row once the transport accepts it.
            await bus.PublishAsync(integrationEvent).ConfigureAwait(false);

            return Result.Ok();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Boundary: cancellation must propagate to the transport.
            throw;
        }
        catch (Exception exception)
        {
            // Catch: transport failures become a retryable outcome for the outbox dispatcher.
            return Result.Fail(EventBusOutcome.Failure.PublishFailed(integrationEvent, exception));
        }
    }
}
