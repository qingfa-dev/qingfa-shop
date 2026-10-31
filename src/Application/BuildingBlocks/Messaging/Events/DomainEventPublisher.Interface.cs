using Core.BuildingBlocks.Domain.Events;

namespace Application.BuildingBlocks.Messaging.Events;

/// <summary>
/// Publishes the domain events collected from tracked aggregates after a unit of work
/// has successfully persisted its changes.
/// </summary>
/// <remarks>
/// The port keeps persistence interceptors independent of the mediator implementation.
/// Implementations must dispatch every event exactly once per successful call; handlers
/// are responsible for idempotency because a failed response cannot be retried atomically
/// with the already-completed save.
/// </remarks>
public interface IDomainEventPublisher
{
    /// <summary>Publishes the supplied domain events.</summary>
    /// <param name="domainEvents">The events to dispatch, in aggregate collection order.</param>
    /// <param name="cancellationToken">A token that cancels dispatch between handlers.</param>
    /// <returns>
    /// A successful result when every event was dispatched; otherwise the failure
    /// reported while publishing.
    /// </returns>
    Task<Result> PublishAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}
