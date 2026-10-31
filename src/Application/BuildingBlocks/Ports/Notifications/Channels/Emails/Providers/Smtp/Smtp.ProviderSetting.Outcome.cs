namespace Application.BuildingBlocks.Ports.Notifications.Channels.Emails.Providers.Smtp;

/// <summary>Error definitions for SMTP provider configuration validation.</summary>
public static class SmtpProviderSettingOutcome
{
    /// <summary>SMTP-specific validation error factories.</summary>
    public static class Failure
    {
        /// <summary>Error: SMTP host is required when enabled.</summary>
        public static Error SmtpHostRequired => Error.Validation(
            code: Codes.SmtpHostRequired,
            message: "SMTP host is required when the provider is enabled.");

        /// <summary>Error: SMTP host exceeds maximum length.</summary>
        public static Error SmtpHostTooLong => Error.Validation(
            code: Codes.SmtpHostTooLong,
            message: $"SMTP host must not exceed {SmtpProviderSettingConstant.Constraints.HostMaxLength} characters.");

        /// <summary>Error: SMTP host format is invalid.</summary>
        public static Error SmtpHostInvalidFormat => Error.Validation(
            code: Codes.SmtpHostInvalidFormat,
            message: "SMTP host must be a valid hostname (e.g., smtp.example.com), IP address, or 'localhost'.");

        /// <summary>Error: SMTP port value is invalid.</summary>
        public static Error SmtpPortInvalid => Error.Validation(
            code: Codes.SmtpPortInvalid,
            message: $"SMTP port must be a positive integer greater than {SmtpProviderSettingConstant.Constraints.PortMin}.");

        /// <summary>Error: SMTP port is out of allowed range.</summary>
        public static Error SmtpPortOutOfRange => Error.Validation(
            code: Codes.SmtpPortOutOfRange,
            message: $"SMTP port must be between {SmtpProviderSettingConstant.Constraints.PortMin} and {SmtpProviderSettingConstant.Constraints.PortMax}.");

        /// <summary>Error: SMTP credentials missing when not using default credentials.</summary>
        public static Error SmtpCredentialsRequired => Error.Validation(
            code: Codes.SmtpCredentialsRequired,
            message: "SMTP username is required when not using default network credentials.");

        /// <summary>Error: SMTP password required when username is provided.</summary>
        public static Error SmtpPasswordRequired => Error.Validation(
            code: Codes.SmtpPasswordRequired,
            message: "SMTP password is required when a username is provided.");

        /// <summary>Error: SMTP username exceeds maximum length.</summary>
        public static Error SmtpUsernameTooLong => Error.Validation(
            code: Codes.SmtpUsernameTooLong,
            message: $"SMTP username must not exceed {SmtpProviderSettingConstant.Constraints.UsernameMaxLength} characters.");

        /// <summary>Error: SMTP password exceeds maximum length.</summary>
        public static Error SmtpPasswordTooLong => Error.Validation(
            code: Codes.SmtpPasswordTooLong,
            message: $"SMTP password must not exceed {SmtpProviderSettingConstant.Constraints.PasswordMaxLength} characters.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string SmtpHostRequired = "Notification.Email.Smtp.Host.Required";
        public const string SmtpHostTooLong = "Notification.Email.Smtp.Host.TooLong";
        public const string SmtpHostInvalidFormat = "Notification.Email.Smtp.Host.InvalidFormat";
        public const string SmtpPortInvalid = "Notification.Email.Smtp.Port.Invalid";
        public const string SmtpPortOutOfRange = "Notification.Email.Smtp.Port.OutOfRange";
        public const string SmtpCredentialsRequired = "Notification.Email.Smtp.Credentials.Required";
        public const string SmtpPasswordRequired = "Notification.Email.Smtp.Password.Required";
        public const string SmtpUsernameTooLong = "Notification.Email.Smtp.Username.TooLong";
        public const string SmtpPasswordTooLong = "Notification.Email.Smtp.Password.TooLong";
    }
}
