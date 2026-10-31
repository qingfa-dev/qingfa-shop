namespace Infrastructure.Payments.Configuration;

/// <summary>Retention settings for the payment module's audit tables.</summary>
public sealed class PaymentRetentionOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Payments:Retention";

    /// <summary>Default number of days a processed webhook delivery is kept.</summary>
    public const int DefaultWebhookEventRetentionDays = 30;

    /// <summary>Default number of hours between retention sweeps.</summary>
    public const int DefaultSweepIntervalHours = 6;

    /// <summary>Whether the retention worker runs.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How long a processed webhook delivery is kept, in days.</summary>
    public int WebhookEventRetentionDays { get; set; } = DefaultWebhookEventRetentionDays;

    /// <summary>How often the retention sweep runs, in hours.</summary>
    public int SweepIntervalHours { get; set; } = DefaultSweepIntervalHours;
}
