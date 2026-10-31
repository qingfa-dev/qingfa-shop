namespace Application.BuildingBlocks.Ports.Notifications.Channels.Emails.Providers.SendGrid;

/// <summary>Error definitions for SendGrid provider configuration validation.</summary>
public static class SendGridProviderSettingOutcome
{
    /// <summary>SendGrid-specific validation error factories.</summary>
    public static class Failure
    {
        /// <summary>API key is required when the provider is enabled.</summary>
        public static Error ApiKeyRequired => Error.Validation(
            code: Codes.ApiKeyRequired,
            message: "SendGrid API key is required when the provider is enabled.");

        /// <summary>API key is shorter than the minimum allowed length.</summary>
        public static Error ApiKeyTooShort => Error.Validation(
            code: Codes.ApiKeyTooShort,
            message: $"SendGrid API key must be at least {SendGridProviderSettingConstant.Constraints.ApiKeyMinLength} characters.");

        /// <summary>API key exceeds the maximum allowed length.</summary>
        public static Error ApiKeyTooLong => Error.Validation(
            code: Codes.ApiKeyTooLong,
            message: $"SendGrid API key must not exceed {SendGridProviderSettingConstant.Constraints.ApiKeyMaxLength} characters.");

        /// <summary>API key does not match the expected format.</summary>
        public static Error ApiKeyInvalidFormat => Error.Validation(
            code: Codes.ApiKeyInvalidFormat,
            message: "SendGrid API key must start with 'SG.' followed by two dot-separated segments of alphanumeric characters.");

        /// <summary>API key contains whitespace characters.</summary>
        public static Error ApiKeyContainsWhitespace => Error.Validation(
            code: Codes.ApiKeyContainsWhitespace,
            message: "SendGrid API key must not contain whitespace characters.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string ApiKeyRequired = "Notification.Email.SendGrid.ApiKey.Required";
        public const string ApiKeyTooShort = "Notification.Email.SendGrid.ApiKey.TooShort";
        public const string ApiKeyTooLong = "Notification.Email.SendGrid.ApiKey.TooLong";
        public const string ApiKeyInvalidFormat = "Notification.Email.SendGrid.ApiKey.InvalidFormat";
        public const string ApiKeyContainsWhitespace = "Notification.Email.SendGrid.ApiKey.ContainsWhitespace";
    }
}
