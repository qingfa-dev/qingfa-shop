namespace Application.BuildingBlocks.Ports.Notifications.Channels.Emails.Providers.SendGrid;

/// <summary>Constants and constraints for SendGrid provider configuration.</summary>
public static class SendGridProviderSettingConstant
{
    /// <summary>Length constraints for the SendGrid API key.</summary>
    public static class Constraints
    {
        public const int ApiKeyMinLength = 10;
        public const int ApiKeyMaxLength = 256;
    }

    /// <summary>Default configuration values for the SendGrid provider.</summary>
    public static class Defaults
    {
        public const string Section = "Notification:Channels:Email:Providers:SendGrid";
        public const int Priority = 2;
    }

    /// <summary>Regex patterns for API key validation.</summary>
    public static class Patterns
    {
        public const string ApiKey = @"^SG\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+$";
    }
}
