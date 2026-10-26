using MediatR;

namespace Core.Common.Domain.Events;

/// <summary>
/// Represents a domain event with correlation and tenant context.
/// Extends <see cref="INotification"/> so domain events can be published via Mediator.
/// </summary>
public interface IDomainEvent : INotification, IHasMetadata
{
}