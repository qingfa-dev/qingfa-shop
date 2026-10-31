namespace Application.BuildingBlocks.Ports.Notifications.Channels.Sms.Providers.Sinch;

/// <summary>Error definitions for Sinch SMS provider configuration validation.</summary>
public static class SinchProviderSettingOutcome
{
    /// <summary>Sinch-specific validation error factories.</summary>
    public static class Failure
    {
        /// <summary>ProjectId is required when the provider is enabled.</summary>
        public static Error ProjectIdRequired => Error.Validation(
            code: Codes.ProjectIdRequired,
            message: "Sinch Project ID is required when the provider is enabled.");

        /// <summary>KeyId is required when the provider is enabled.</summary>
        public static Error KeyIdRequired => Error.Validation(
            code: Codes.KeyIdRequired,
            message: "Sinch Key ID is required when the provider is enabled.");

        /// <summary>KeySecret is required when the provider is enabled.</summary>
        public static Error KeySecretRequired => Error.Validation(
            code: Codes.KeySecretRequired,
            message: "Sinch Key Secret is required when the provider is enabled.");

        /// <summary>SenderPhoneNumber is required when the provider is enabled.</summary>
        public static Error SenderPhoneNumberRequired => Error.Validation(
            code: Codes.SenderPhoneNumberRequired,
            message: "Sinch Sender Phone Number is required when the provider is enabled.");

        /// <summary>ProjectId length is outside the allowed range.</summary>
        public static Error ProjectIdInvalidLength => Error.Validation(
            code: Codes.ProjectIdInvalidLength,
            message: $"Sinch Project ID must be between {SinchProviderSettingConstant.Constraints.ProjectIdMinLength} and {SinchProviderSettingConstant.Constraints.ProjectIdMaxLength} characters.");

        /// <summary>KeyId length is outside the allowed range.</summary>
        public static Error KeyIdInvalidLength => Error.Validation(
            code: Codes.KeyIdInvalidLength,
            message: $"Sinch Key ID must be between {SinchProviderSettingConstant.Constraints.KeyIdMinLength} and {SinchProviderSettingConstant.Constraints.KeyIdMaxLength} characters.");

        /// <summary>KeySecret length is outside the allowed range.</summary>
        public static Error KeySecretInvalidLength => Error.Validation(
            code: Codes.KeySecretInvalidLength,
            message: $"Sinch Key Secret must be between {SinchProviderSettingConstant.Constraints.KeySecretMinLength} and {SinchProviderSettingConstant.Constraints.KeySecretMaxLength} characters.");

        /// <summary>Sender phone number format is invalid.</summary>
        public static Error SenderPhoneNumberInvalid => Error.Validation(
            code: Codes.SenderPhoneNumberInvalid,
            message: "Sender phone number must be in E.164 format (e.g., +1234567890).");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string ProjectIdRequired = "Notification.Sms.Sinch.ProjectId.Required";
        public const string KeyIdRequired = "Notification.Sms.Sinch.KeyId.Required";
        public const string KeySecretRequired = "Notification.Sms.Sinch.KeySecret.Required";
        public const string SenderPhoneNumberRequired = "Notification.Sms.Sinch.SenderPhoneNumber.Required";
        public const string ProjectIdInvalidLength = "Notification.Sms.Sinch.ProjectId.InvalidLength";
        public const string KeyIdInvalidLength = "Notification.Sms.Sinch.KeyId.InvalidLength";
        public const string KeySecretInvalidLength = "Notification.Sms.Sinch.KeySecret.InvalidLength";
        public const string SenderPhoneNumberInvalid = "Notification.Sms.Sinch.SenderPhoneNumber.InvalidFormat";
    }
}
