namespace Application.BuildingBlocks.Ports.Notifications.Channels.Emails.Options;

/// <summary>
/// Contains Error definitions specific to the email channel.
/// </summary>
public static class EmailChannelSettingOutcome
{
    /// <summary>
    /// Email channel specific Error.
    /// </summary>
    public static class Failure
    {
        /// <summary>Thrown when the sender email is missing.</summary>
        public static Error FromEmailRequired => Error.Validation(
            code: Codes.FromEmailRequired,
            message: "Sender email is required.");

        /// <summary>Thrown when the sender name is missing.</summary>
        public static Error FromNameRequired => Error.Validation(
            code: Codes.FromNameRequired,
            message: "Sender name is required.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string FromEmailRequired = "Notification.Email.FromEmailRequired";
        public const string FromNameRequired = "Notification.Email.FromNameRequired";
    }
}
