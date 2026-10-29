namespace Application.Common.Integrations.Notifications.Channels.Sms.Providers.Sinch;

/// <summary>Constants and constraints for Sinch SMS provider configuration.</summary>
public static class SinchProviderSettingConstant
{
    /// <summary>Length constraints for Sinch provider configuration values.</summary>
    public static class Constraints
    {
        public const int ProjectIdMinLength = 1;
        public const int ProjectIdMaxLength = 64;
        public const int KeyIdMinLength = 1;
        public const int KeyIdMaxLength = 128;
        public const int KeySecretMinLength = 1;
        public const int KeySecretMaxLength = 256;
        public const int SenderPhoneNumberMaxLength = 20;
    }

    /// <summary>Default configuration values for the Sinch provider.</summary>
    public static class Defaults
    {
        public const string Section = "Notification:Channels:Sms:Providers:Sinch";
        public const int Priority = 1;
    }

    /// <summary>Regex patterns for Sinch configuration validation.</summary>
    public static class Patterns
    {
        public const string SenderPhoneNumber = @"^\+[1-9]\d{1,14}$";
    }
}
