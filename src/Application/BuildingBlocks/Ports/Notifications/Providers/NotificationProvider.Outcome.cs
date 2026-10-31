namespace Application.BuildingBlocks.Ports.Notifications.Providers;

/// <summary>Error definitions for notification provider operations.</summary>
public static class NotificationProviderOutcome
{
    /// <summary>Provider-level error factories for send failures and configuration issues.</summary>
    public static class Failure
    {
        /// <summary>A provider failed to send a notification due to an unexpected error.</summary>
        public static Error SendFailed(string providerName, string error)
            => Error.Unexpected(
                code: $"Provider.{providerName}.SendFailed",
                message: $"Provider {providerName} failed to send message: {error}");

        /// <summary>A required configuration value is missing for the provider.</summary>
        public static Error ConfigurationMissing(string providerName, string field)
            => Error.Unexpected(
                code: $"Provider.{providerName}.Configuration.{field}.Required",
                message: $"Provider {providerName} requires configuration field '{field}'.");

        /// <summary>The recipient identifier is missing or empty.</summary>
        public static Error RecipientMissing(string providerName)
            => Error.Validation(
                code: $"Provider.{providerName}.Recipient.Required",
                message: $"Provider {providerName} requires a recipient identifier.");
    }
}
