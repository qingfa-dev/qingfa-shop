namespace Application.Common.Integrations.Notifications.Channels.Emails.Providers.SendGrid;

/// <summary>Error definitions for SendGrid provider configuration validation.</summary>
public static class SendGridProviderSettingResult
{
    /// <summary>SendGrid-specific validation error factories.</summary>
    public static class Failure
    {
        /// <summary>API key is required when the provider is enabled.</summary>
        public static Error ApiKeyRequired => Error.Validation(
            code: "Notification.Email.SendGrid.ApiKey.Required",
            message: "SendGrid API key is required when the provider is enabled.");

        /// <summary>API key is shorter than the minimum allowed length.</summary>
        public static Error ApiKeyTooShort => Error.Validation(
            code: "Notification.Email.SendGrid.ApiKey.TooShort",
            message: $"SendGrid API key must be at least {SendGridProviderSettingConstant.Constraints.ApiKeyMinLength} characters.");

        /// <summary>API key exceeds the maximum allowed length.</summary>
        public static Error ApiKeyTooLong => Error.Validation(
            code: "Notification.Email.SendGrid.ApiKey.TooLong",
            message: $"SendGrid API key must not exceed {SendGridProviderSettingConstant.Constraints.ApiKeyMaxLength} characters.");

        /// <summary>API key does not match the expected format.</summary>
        public static Error ApiKeyInvalidFormat => Error.Validation(
            code: "Notification.Email.SendGrid.ApiKey.InvalidFormat",
            message: "SendGrid API key must start with 'SG.' followed by two dot-separated segments of alphanumeric characters.");

        /// <summary>API key contains whitespace characters.</summary>
        public static Error ApiKeyContainsWhitespace => Error.Validation(
            code: "Notification.Email.SendGrid.ApiKey.ContainsWhitespace",
            message: "SendGrid API key must not contain whitespace characters.");
    }
}
