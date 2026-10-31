namespace Application.BuildingBlocks.Ports.Notifications.Channels.Sms.Options;

/// <summary>Error definitions for SMS channel configuration validation.</summary>
public static class SmsChannelSettingOutcome
{
    /// <summary>SMS channel validation error factories.</summary>
    public static class Failure
    {
        /// <summary>At least one recipient is required.</summary>
        public static Error ToRequired => Error.Validation(
            code: Codes.ToRequired,
            message: "At least one recipient is required.");

        /// <summary>Invalid phone number format.</summary>
        public static Error InvalidFormat => Error.Validation(
            code: Codes.InvalidFormat,
            message: "Invalid phone number format.");

        /// <summary>SMS body is required.</summary>
        public static Error BodyRequired => Error.Validation(
            code: Codes.BodyRequired,
            message: "SMS body is required.");

        /// <summary>SMS body exceeds maximum length.</summary>
        public static Error BodyTooLong => Error.Validation(
            code: Codes.BodyTooLong,
            message: "SMS body cannot exceed 160 characters.");

        /// <summary>Failed to send the SMS message.</summary>
        public static Error SendFailed(string details) => Error.Unexpected(
            code: Codes.SendFailed,
            message: $"Failed to send SMS: {details}");

        /// <summary>No active SMS providers are configured.</summary>
        public static Error NoProvidersConfigured => Error.Unexpected(
            code: Codes.NoProvidersConfigured,
            message: "No active SMS providers are configured.");

        /// <summary>All configured SMS providers failed to send.</summary>
        public static Error AllProvidersFailed => Error.Unexpected(
            code: Codes.AllProvidersFailed,
            message: "All SMS providers failed to send the notification.");

        /// <summary>Default sender number is required.</summary>
        public static Error DefaultSenderNumberRequired => Error.Validation(
            code: Codes.DefaultSenderNumberRequired,
            message: "Default sender number is required.");

        /// <summary>Default sender number format is invalid.</summary>
        public static Error DefaultSenderNumberInvalid => Error.Validation(
            code: Codes.DefaultSenderNumberInvalid,
            message: "Default sender number must be in E.164 format (e.g., +1234567890).");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string ToRequired = "Notification.Sms.ToRequired";
        public const string InvalidFormat = "Notification.Sms.InvalidFormat";
        public const string BodyRequired = "Notification.Sms.BodyRequired";
        public const string BodyTooLong = "Notification.Sms.BodyTooLong";
        public const string SendFailed = "Notification.Sms.SendFailed";
        public const string NoProvidersConfigured = "Notification.Sms.NoProvidersConfigured";
        public const string AllProvidersFailed = "Notification.Sms.AllProvidersFailed";
        public const string DefaultSenderNumberRequired = "Notification.Sms.DefaultSenderNumber.Required";
        public const string DefaultSenderNumberInvalid = "Notification.Sms.DefaultSenderNumber.InvalidFormat";
    }
}
