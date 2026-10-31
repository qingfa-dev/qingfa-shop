using Application.BuildingBlocks.Messaging.Events;

using Core.BuildingBlocks.Domain.Events;

using MediatR;

namespace Infrastructure.BuildingBlocks.Adapters.Events;

/// <summary>
/// Dispatches domain events through MediatR's notification pipeline.
/// </summary>
/// <remarks>
/// Each event is published using its runtime type, so handlers registered for the concrete
/// notification receive it. The publisher stops at the first failing handler and returns the
/// failure with the originating exception metadata.
/// </remarks>
public sealed class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IPublisher _publisher;

    /// <summary>Creates a MediatR-backed domain-event publisher.</summary>
    /// <param name="publisher">The mediator notification publisher.</param>
    public DomainEventPublisher(IPublisher publisher)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    /// <inheritdoc />
    public async Task<Result> PublishAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (IDomainEvent domainEvent in domainEvents)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);

            try
            {
                // Publish: cast to object so MediatR dispatches by runtime type instead of IDomainEvent.
                await _publisher.Publish((object)domainEvent, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Boundary: preserve cancellation for the caller.
                throw;
            }
            catch (Exception exception)
            {
                return Result.Fail(DomainEventOutcome.Failure.PublishFailed(domainEvent, exception));
            }
        }

        return Result.Ok();
    }
}
