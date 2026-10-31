using Core.BuildingBlocks.Domain.Events;

namespace Application.BuildingBlocks.Messaging.Events;

/// <summary>
/// Failures produced while publishing domain events.
/// </summary>
/// <remarks>
/// Domain-event dispatch happens after persistence has succeeded, so its failures do not
/// describe a storage problem. They are kept separate from
/// <see cref="Persistence.UnitOfWorks.UnitOfWorkOutcome"/> and
/// <see cref="Persistence.UnitOfWorks.TransactionOutcome"/> to preserve ownership.
/// </remarks>
public static class DomainEventOutcome
{
    /// <summary>Failure outcomes for domain-event publication.</summary>
    public static class Failure
    {
        /// <summary>The publisher could not dispatch an event.</summary>
        /// <param name="eventType">The concrete event type that failed.</param>
        /// <param name="reason">The handler or mediator failure.</param>
        /// <returns>An internal-server error carrying the failure reason.</returns>
        public static Error PublishFailed(Type eventType, string reason) => Error.InternalServerError(
            Codes.PublishFailed,
            $"Domain event '{eventType.Name}' could not be published: {reason}");

        /// <summary>The publisher could not dispatch an event.</summary>
        /// <param name="domainEvent">The event that failed.</param>
        /// <param name="exception">The exception raised by the handler or mediator.</param>
        /// <returns>An internal-server error with the originating exception metadata.</returns>
        public static Error PublishFailed(IDomainEvent domainEvent, Exception exception)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);
            ArgumentNullException.ThrowIfNull(exception);

            return PublishFailed(domainEvent.GetType(), exception.Message)
                .WithMetadata(ErrorConstant.Metadata.ExceptionType, exception.GetType().FullName ?? exception.GetType().Name)
                .WithMetadata(ErrorConstant.Metadata.ExceptionMessage, exception.Message);
        }
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        /// <summary>A handler or the mediator rejected the event.</summary>
        public const string PublishFailed = "DomainEvent.Publish.Failed";
    }
}
