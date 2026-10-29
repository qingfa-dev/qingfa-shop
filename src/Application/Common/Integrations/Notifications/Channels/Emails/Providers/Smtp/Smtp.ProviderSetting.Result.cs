namespace Application.Common.Integrations.Notifications.Channels.Emails.Providers.Smtp;

/// <summary>Error definitions for SMTP provider configuration validation.</summary>
public static class SmtpProviderSettingResult
{
    /// <summary>SMTP-specific validation error factories.</summary>
    public static class Failure
    {
        /// <summary>Error: SMTP host is required when enabled.</summary>
        public static Error SmtpHostRequired => Error.Validation(
            code: "Notification.Email.Smtp.Host.Required",
            message: "SMTP host is required when the provider is enabled.");

        /// <summary>Error: SMTP host exceeds maximum length.</summary>
        public static Error SmtpHostTooLong => Error.Validation(
            code: "Notification.Email.Smtp.Host.TooLong",
            message: $"SMTP host must not exceed {SmtpProviderSettingConstant.Constraints.HostMaxLength} characters.");

        /// <summary>Error: SMTP host format is invalid.</summary>
        public static Error SmtpHostInvalidFormat => Error.Validation(
            code: "Notification.Email.Smtp.Host.InvalidFormat",
            message: "SMTP host must be a valid hostname (e.g., smtp.example.com), IP address, or 'localhost'.");

        /// <summary>Error: SMTP port value is invalid.</summary>
        public static Error SmtpPortInvalid => Error.Validation(
            code: "Notification.Email.Smtp.Port.Invalid",
            message: $"SMTP port must be a positive integer greater than {SmtpProviderSettingConstant.Constraints.PortMin}.");

        /// <summary>Error: SMTP port is out of allowed range.</summary>
        public static Error SmtpPortOutOfRange => Error.Validation(
            code: "Notification.Email.Smtp.Port.OutOfRange",
            message: $"SMTP port must be between {SmtpProviderSettingConstant.Constraints.PortMin} and {SmtpProviderSettingConstant.Constraints.PortMax}.");

        /// <summary>Error: SMTP credentials missing when not using default credentials.</summary>
        public static Error SmtpCredentialsRequired => Error.Validation(
            code: "Notification.Email.Smtp.Credentials.Required",
            message: "SMTP username is required when not using default network credentials.");

        /// <summary>Error: SMTP password required when username is provided.</summary>
        public static Error SmtpPasswordRequired => Error.Validation(
            code: "Notification.Email.Smtp.Password.Required",
            message: "SMTP password is required when a username is provided.");

        /// <summary>Error: SMTP username exceeds maximum length.</summary>
        public static Error SmtpUsernameTooLong => Error.Validation(
            code: "Notification.Email.Smtp.Username.TooLong",
            message: $"SMTP username must not exceed {SmtpProviderSettingConstant.Constraints.UsernameMaxLength} characters.");

        /// <summary>Error: SMTP password exceeds maximum length.</summary>
        public static Error SmtpPasswordTooLong => Error.Validation(
            code: "Notification.Email.Smtp.Password.TooLong",
            message: $"SMTP password must not exceed {SmtpProviderSettingConstant.Constraints.PasswordMaxLength} characters.");
    }
}
