using Application.BuildingBlocks.Messaging.Events;

using Core.BuildingBlocks.Domain.Events;

namespace Infrastructure.BuildingBlocks.Adapters.Events;

/// <summary>
/// A publisher that accepts events without dispatching them.
/// </summary>
/// <remarks>
/// Used when the persistence stack is composed without MediatR (for example, a database
/// initialization or migration host). Hosts that expose domain-event handlers register
/// MediatR and receive <see cref="DomainEventPublisher"/> instead.
/// </remarks>
internal sealed class NullDomainEventPublisher : IDomainEventPublisher
{
    /// <summary>The shared stateless instance.</summary>
    public static readonly NullDomainEventPublisher Instance = new();

    private NullDomainEventPublisher()
    {
    }

    /// <inheritdoc />
    public Task<Result> PublishAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        return Task.FromResult(Result.Ok());
    }
}
