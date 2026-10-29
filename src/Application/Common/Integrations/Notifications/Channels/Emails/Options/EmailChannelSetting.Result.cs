namespace Application.Common.Integrations.Notifications.Channels.Emails.Options;

/// <summary>
/// Contains Error definitions specific to the email channel.
/// </summary>
public static class EmailChannelSettingResult
{
    /// <summary>
    /// Email channel specific Error.
    /// </summary>
    public static class Failure
    {
        /// <summary>Thrown when the sender email is missing.</summary>
        public static Error FromEmailRequired => Error.Validation(
            code: "Notification.Email.FromEmailRequired",
            message: "Sender email is required.");

        /// <summary>Thrown when the sender name is missing.</summary>
        public static Error FromNameRequired => Error.Validation(
            code: "Notification.Email.FromNameRequired",
            message: "Sender name is required.");
    }
}
