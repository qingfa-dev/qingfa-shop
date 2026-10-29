namespace Application.Common.Integrations.Notifications.Channels.Sms.Options;

/// <summary>Constants and constraints for SMS channel configuration.</summary>
public static class SmsChannelSettingConstant
{
    /// <summary>Constraint boundaries for SMS configuration values.</summary>
    public static class Constraints
    {
        public const int DefaultSenderNumberMaxLength = 20;
    }

    /// <summary>Default configuration values for the SMS channel.</summary>
    public static class Defaults
    {
        public const string Section = "Notification:Channels:Sms";
    }

    public static class Patterns
    {
        public const string SenderNumber = @"^\+[1-9]\d{1,14}$";
    }
}
