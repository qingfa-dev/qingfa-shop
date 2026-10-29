namespace Application.Common.Integrations.Notifications.Options;

/// <summary>Constants, defaults, constraints, and patterns for notification system configuration.</summary>
public static class NotificationSettingConstant
{
    /// <summary>Default configuration values for the notification system.</summary>
    public static class Defaults
    {
        public const string ApplicationName = "ReSys Shop";
        public const string SupportEmail = "support@resys.shop";
        public const string SupportPhone = "+1-000-000-0000";
        public const string CustomerSupportLink = "https://resys.shop/support";
        public const string ApplicationUrl = "https://resys.shop";
        public const string UnsubscribeUrl = "https://resys.shop/unsubscribe";
        public const string SurveyUrl = "https://resys.shop/survey";
    }

    /// <summary>Constraint boundaries for notification configuration values.</summary>
    public static class Constraints
    {
        public const int MinApplicationNameLength = 1;
        public const int MaxApplicationNameLength = 100;
    }

    /// <summary>Regex patterns for notification configuration validation.</summary>
    public static class Patterns
    {
        public const string PhoneNumber = @"^\+?[\d\s\-()]+$";
    }


    /// <summary>
    /// Constants related to background job processing.
    /// </summary>
    public static class BackgroundJobs
    {
        public const string ServerName = "Notifications-Worker-Server";

        /// <summary>Queue names for Hangfire background job prioritisation.</summary>
        public static class Queues
        {
            public const string Critical = "critical";
            public const string High = "high";
            public const string Default = "default";
            public const string Low = "low";

            public static string[] All => [Critical, High, Default, Low];
        }
    }
}
