namespace Application.Common.Integrations.Notifications.Channels.Sms.Options;

/// <summary>Error definitions for SMS channel configuration validation.</summary>
public static class SmsChannelSettingResult
{
    /// <summary>SMS channel validation error factories.</summary>
    public static class Failure
    {
        /// <summary>At least one recipient is required.</summary>
        public static Error ToRequired => Error.Validation(
            code: "Notification.Sms.ToRequired",
            message: "At least one recipient is required.");

        /// <summary>Invalid phone number format.</summary>
        public static Error InvalidFormat => Error.Validation(
            code: "Notification.Sms.InvalidFormat",
            message: "Invalid phone number format.");

        /// <summary>SMS body is required.</summary>
        public static Error BodyRequired => Error.Validation(
            code: "Notification.Sms.BodyRequired",
            message: "SMS body is required.");

        /// <summary>SMS body exceeds maximum length.</summary>
        public static Error BodyTooLong => Error.Validation(
            code: "Notification.Sms.BodyTooLong",
            message: "SMS body cannot exceed 160 characters.");

        /// <summary>Failed to send the SMS message.</summary>
        public static Error SendFailed(string details) => Error.Unexpected(
            code: "Notification.Sms.SendFailed",
            message: $"Failed to send SMS: {details}");

        /// <summary>No active SMS providers are configured.</summary>
        public static Error NoProvidersConfigured => Error.Unexpected(
            code: "Notification.Sms.NoProvidersConfigured",
            message: "No active SMS providers are configured.");

        /// <summary>All configured SMS providers failed to send.</summary>
        public static Error AllProvidersFailed => Error.Unexpected(
            code: "Notification.Sms.AllProvidersFailed",
            message: "All SMS providers failed to send the notification.");

        /// <summary>Default sender number is required.</summary>
        public static Error DefaultSenderNumberRequired => Error.Validation(
            code: "Notification.Sms.DefaultSenderNumber.Required",
            message: "Default sender number is required.");

        /// <summary>Default sender number format is invalid.</summary>
        public static Error DefaultSenderNumberInvalid => Error.Validation(
            code: "Notification.Sms.DefaultSenderNumber.InvalidFormat",
            message: "Default sender number must be in E.164 format (e.g., +1234567890).");
    }
}
