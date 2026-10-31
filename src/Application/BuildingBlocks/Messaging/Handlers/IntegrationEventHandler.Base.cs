using Application.BuildingBlocks.Messaging.Bus;
using Application.BuildingBlocks.Messaging.Inbox;

namespace Application.BuildingBlocks.Messaging.Handlers;

/// <summary>Runs an integration-event consumer at most once per message id and consumer type.</summary>
/// <remarks>
/// The inbox owns the transaction. A duplicate delivery returns success without invoking
/// <see cref="HandleAsync"/>, and a failed consumer rolls its writes back before the failure is
/// thrown, so the transport can retry the delivery.
/// </remarks>
/// <typeparam name="TEvent">The integration event handled by the consumer.</typeparam>
public abstract class IntegrationEventHandler<TEvent>
    where TEvent : class, IIntegrationEvent
{
    private readonly IInboxStore _inbox;

    /// <summary>Creates a consumer that dedupes deliveries through the supplied inbox.</summary>
    /// <param name="inbox">The inbox that owns the dedupe marker and the transaction.</param>
    protected IntegrationEventHandler(IInboxStore inbox)
    {
        _inbox = inbox ?? throw new ArgumentNullException(nameof(inbox));
    }

    /// <summary>Handles the event, skipping a delivery already recorded for this consumer.</summary>
    /// <param name="message">The integration event to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ResultException">
    /// Thrown after the inbox rolls back a failed consumer, so the transport retries the delivery.
    /// </exception>
    protected async Task ExecuteOnceAsync(
        TEvent message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        Result result = await _inbox
            .ExecuteOnceAsync(
                message.MessageId,
                GetType().FullName ?? GetType().Name,
                token => HandleAsync(message, token),
                cancellationToken)
            .ConfigureAwait(false);

        // Retry: the inbox already rolled back, so throwing keeps the message eligible for redelivery.
        result.ThrowIfFailure();
    }

    /// <summary>Runs the consumer logic after the inbox accepts the message.</summary>
    /// <param name="message">The integration event to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A successful result when the consumer's effects are complete; otherwise a failure.</returns>
    protected abstract Task<Result> HandleAsync(
        TEvent message,
        CancellationToken cancellationToken);
}