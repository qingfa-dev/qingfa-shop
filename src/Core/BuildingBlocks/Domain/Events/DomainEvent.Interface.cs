using MediatR;

namespace Core.BuildingBlocks.Domain.Events;

/// <summary>
/// Represents a domain event with correlation and tenant context.
/// Extends <see cref="INotification"/> so domain events can be published via Mediator.
/// </summary>
public interface IDomainEvent : INotification, IHasMetadata
{
    /// <summary>Gets the stable wire name of the event.</summary>
    string EventName { get; }

    /// <summary>Gets the payload version of the event.</summary>
    int EventVersion { get; }
}
