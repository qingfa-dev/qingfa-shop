namespace Application.Common.Integrations.Notifications.Options.Providers;

/// <summary>Constants, defaults, and constraints for provider configuration.</summary>
public static class ProviderSettingConstant
{
    /// <summary>Default values for provider configuration properties.</summary>
    public static class Defaults
    {
        public const bool Enabled = true;
        public const int Priority = 1;
        public const int RetryCount = 3;
        public const int TimeoutSeconds = 30;
    }

    public static TimeSpan DefaultTimeout => TimeSpan.FromSeconds(Defaults.TimeoutSeconds);

    /// <summary>Constraint boundaries for provider configuration values.</summary>
    public static class Constraints
    {
        public const int MinPriority = 1;
        public const int MaxPriority = 100;
        public const int MinRetryCount = 0;
        public const int MaxRetryCount = 10;
        public const int MinTimeoutSeconds = 1;
        public const int MaxTimeoutSeconds = 300;
    }
}
