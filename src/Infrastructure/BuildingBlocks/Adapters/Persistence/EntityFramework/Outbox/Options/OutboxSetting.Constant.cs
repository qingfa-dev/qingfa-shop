namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox.Options;

/// <summary>Default values for <see cref="OutboxSetting"/>.</summary>
public static class OutboxSettingConstant
{
    /// <summary>Default settings.</summary>
    public static class Defaults
    {
        public const bool Enabled = true;
        public const int BatchSize = 100;
        public const int PollingIntervalSeconds = 2;
        public const int MaxAttempts = 10;
        public const int BackoffSeconds = 5;
        public const int MaxBackoffSeconds = 300;
        public const int LeaseSeconds = 60;
        public const int RetentionDays = 7;
        public const int MaxAgeSeconds = 300;
    }
}
