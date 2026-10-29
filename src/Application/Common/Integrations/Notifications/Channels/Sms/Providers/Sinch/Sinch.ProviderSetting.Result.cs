namespace Application.Common.Integrations.Notifications.Channels.Sms.Providers.Sinch;

/// <summary>Error definitions for Sinch SMS provider configuration validation.</summary>
public static class SinchProviderSettingResult
{
    /// <summary>Sinch-specific validation error factories.</summary>
    public static class Failure
    {
        /// <summary>ProjectId is required when the provider is enabled.</summary>
        public static Error ProjectIdRequired => Error.Validation(
            code: "Notification.Sms.Sinch.ProjectId.Required",
            message: "Sinch Project ID is required when the provider is enabled.");

        /// <summary>KeyId is required when the provider is enabled.</summary>
        public static Error KeyIdRequired => Error.Validation(
            code: "Notification.Sms.Sinch.KeyId.Required",
            message: "Sinch Key ID is required when the provider is enabled.");

        /// <summary>KeySecret is required when the provider is enabled.</summary>
        public static Error KeySecretRequired => Error.Validation(
            code: "Notification.Sms.Sinch.KeySecret.Required",
            message: "Sinch Key Secret is required when the provider is enabled.");

        /// <summary>SenderPhoneNumber is required when the provider is enabled.</summary>
        public static Error SenderPhoneNumberRequired => Error.Validation(
            code: "Notification.Sms.Sinch.SenderPhoneNumber.Required",
            message: "Sinch Sender Phone Number is required when the provider is enabled.");

        /// <summary>ProjectId length is outside the allowed range.</summary>
        public static Error ProjectIdInvalidLength => Error.Validation(
            code: "Notification.Sms.Sinch.ProjectId.InvalidLength",
            message: $"Sinch Project ID must be between {SinchProviderSettingConstant.Constraints.ProjectIdMinLength} and {SinchProviderSettingConstant.Constraints.ProjectIdMaxLength} characters.");

        /// <summary>KeyId length is outside the allowed range.</summary>
        public static Error KeyIdInvalidLength => Error.Validation(
            code: "Notification.Sms.Sinch.KeyId.InvalidLength",
            message: $"Sinch Key ID must be between {SinchProviderSettingConstant.Constraints.KeyIdMinLength} and {SinchProviderSettingConstant.Constraints.KeyIdMaxLength} characters.");

        /// <summary>KeySecret length is outside the allowed range.</summary>
        public static Error KeySecretInvalidLength => Error.Validation(
            code: "Notification.Sms.Sinch.KeySecret.InvalidLength",
            message: $"Sinch Key Secret must be between {SinchProviderSettingConstant.Constraints.KeySecretMinLength} and {SinchProviderSettingConstant.Constraints.KeySecretMaxLength} characters.");

        /// <summary>Sender phone number format is invalid.</summary>
        public static Error SenderPhoneNumberInvalid => Error.Validation(
            code: "Notification.Sms.Sinch.SenderPhoneNumber.InvalidFormat",
            message: "Sender phone number must be in E.164 format (e.g., +1234567890).");
    }
}
