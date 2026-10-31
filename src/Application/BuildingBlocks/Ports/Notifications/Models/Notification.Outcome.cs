namespace Application.BuildingBlocks.Ports.Notifications.Models;

/// <summary>
/// Provides a centralized set of Error definitions for the notification building block.
/// </summary>
public static class NotificationOutcome
{
    /// <summary>
    /// Contains error definitions grouped by channel and general logic.
    /// </summary>
    public static class Failure
    {

        /// <summary>Thrown when the requested UseCase is not defined in the Metadata Store.</summary>
        public static Error TemplateNotFound(string useCase) => Error.NotFound(
            code: Codes.TemplateNotFound,
            message: $"Template for use case '{useCase}' not found.");

        /// <summary>Thrown when a template specifies a delivery method that is not implemented.</summary>
        public static Error UnsupportedMethod => Error.Validation(
            code: Codes.UnsupportedMethod,
            message: "The template specifies an unsupported send method.");

        /// <summary>A catch-all for unexpected internal exceptions.</summary>
        public static Error Unexpected(string message) => Error.Unexpected(
            code: Codes.Unexpected,
            message: message);

        /// <summary>Thrown when a request is made without a UseCase.</summary>
        public static Error UseCaseRequired => Error.Validation(
            code: Codes.UseCaseRequired,
            message: "UseCase is required.");

        /// <summary>Thrown when a request is made without a recipient.</summary>
        public static Error RecipientRequired => Error.Validation(
            code: Codes.RecipientRequired,
            message: "Recipient is required.");

        /// <summary>Thrown when background jobs are enabled but Hangfire is not registered.</summary>
        public static Error BackgroundJobClientNotConfigured(string useCase) => Error.Validation(
            code: Codes.BackgroundJobClientNotConfigured,
            message: $"Background jobs are enabled for '{useCase}' but no IBackgroundJobClient is registered. Enable Hangfire or disable background jobs.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string TemplateNotFound = "Notification.TemplateNotFound";
        public const string UnsupportedMethod = "Notification.UnsupportedMethod";
        public const string Unexpected = "Notification.Unexpected";
        public const string UseCaseRequired = "Notification.UseCaseRequired";
        public const string RecipientRequired = "Notification.RecipientRequired";
        public const string BackgroundJobClientNotConfigured = "Notification.BackgroundJobClientNotConfigured";
    }
}
