namespace Infrastructure.Persistence.EntityFramework.Outbox.Options;

/// <summary>Configuration for the durable outbox dispatcher.</summary>
public sealed class OutboxSetting
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Outbox";

    /// <summary>Gets or sets whether the dispatcher and worker are active.</summary>
    public bool Enabled { get; set; } = OutboxSettingConstant.Defaults.Enabled;

    /// <summary>Gets or sets the maximum number of rows claimed per batch.</summary>
    public int BatchSize { get; set; } = OutboxSettingConstant.Defaults.BatchSize;

    /// <summary>Gets or sets the worker polling interval in seconds.</summary>
    public int PollingIntervalSeconds { get; set; } = OutboxSettingConstant.Defaults.PollingIntervalSeconds;

    /// <summary>Gets or sets the maximum delivery attempts before dead-lettering.</summary>
    public int MaxAttempts { get; set; } = OutboxSettingConstant.Defaults.MaxAttempts;

    /// <summary>Gets or sets the base exponential backoff in seconds.</summary>
    public int BackoffSeconds { get; set; } = OutboxSettingConstant.Defaults.BackoffSeconds;

    /// <summary>Gets or sets the maximum backoff in seconds.</summary>
    public int MaxBackoffSeconds { get; set; } = OutboxSettingConstant.Defaults.MaxBackoffSeconds;

    /// <summary>Gets or sets the claim lease duration in seconds.</summary>
    public int LeaseSeconds { get; set; } = OutboxSettingConstant.Defaults.LeaseSeconds;

    /// <summary>Gets or sets how many days processed rows are retained.</summary>
    public int RetentionDays { get; set; } = OutboxSettingConstant.Defaults.RetentionDays;

    /// <summary>Gets or sets the age in seconds at which pending work is reported as degraded.</summary>
    public int MaxAgeSeconds { get; set; } = OutboxSettingConstant.Defaults.MaxAgeSeconds;
}
