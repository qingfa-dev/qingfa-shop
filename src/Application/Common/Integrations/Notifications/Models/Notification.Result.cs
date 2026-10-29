namespace Application.Common.Integrations.Notifications.Models;

/// <summary>
/// Provides a centralized set of Error definitions for the notification building block.
/// </summary>
public static class NotificationResult
{
    /// <summary>
    /// Contains error definitions grouped by channel and general logic.
    /// </summary>
    public static class Failure
    {

        /// <summary>Thrown when the requested UseCase is not defined in the Metadata Store.</summary>
        public static Error TemplateNotFound(string useCase) => Error.NotFound(
            code: "Notification.TemplateNotFound",
            message: $"Template for use case '{useCase}' not found.");

        /// <summary>Thrown when a template specifies a delivery method that is not implemented.</summary>
        public static Error UnsupportedMethod => Error.Validation(
            code: "Notification.UnsupportedMethod",
            message: "The template specifies an unsupported send method.");

        /// <summary>A catch-all for unexpected internal exceptions.</summary>
        public static Error Unexpected(string message) => Error.Unexpected(
            code: "Notification.Unexpected",
            message: message);

        /// <summary>Thrown when a request is made without a UseCase.</summary>
        public static Error UseCaseRequired => Error.Validation(
            code: "Notification.UseCaseRequired",
            message: "UseCase is required.");

        /// <summary>Thrown when a request is made without a recipient.</summary>
        public static Error RecipientRequired => Error.Validation(
            code: "Notification.RecipientRequired",
            message: "Recipient is required.");

        /// <summary>Thrown when background jobs are enabled but Hangfire is not registered.</summary>
        public static Error BackgroundJobClientNotConfigured(string useCase) => Error.Validation(
            code: "Notification.BackgroundJobClientNotConfigured",
            message: $"Background jobs are enabled for '{useCase}' but no IBackgroundJobClient is registered. Enable Hangfire or disable background jobs.");
    }
}
