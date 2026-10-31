namespace Application.BuildingBlocks.Messaging.Bus;

/// <summary>Failures produced by the event bus.</summary>
public static class EventBusOutcome
{
    /// <summary>Failure factories for the event bus.</summary>
    public static class Failure
    {
        /// <summary>Creates the failure returned when an integration event cannot be published.</summary>
        /// <param name="integrationEvent">The event that failed to publish.</param>
        /// <param name="exception">The exception raised by the transport.</param>
        /// <returns>An error carrying the transport exception.</returns>
        public static Error PublishFailed(
            IIntegrationEvent integrationEvent,
            Exception exception)
        {
            ArgumentNullException.ThrowIfNull(integrationEvent);

            return Error.FromException(exception, Codes.PublishFailed)
                .WithMetadata("event.name", integrationEvent.EventName)
                .WithMetadata("event.messageId", integrationEvent.MessageId);
        }

        /// <summary>Creates the failure returned when publishing is disabled.</summary>
        /// <returns>An error describing the disabled event bus.</returns>
        public static Error Disabled() => Error.InternalServerError(
            Codes.Disabled,
            "The event bus is disabled; no integration event was published.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string PublishFailed = "EventBus.Publish.Failed";
        public const string Disabled = "EventBus.Disabled";
    }
}