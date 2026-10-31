namespace Infrastructure.Modules.Payments;

/// <summary>Bounds applied when the payments configuration is validated.</summary>
public static class PaymentsConfigurationConstant
{
    /// <summary>Smallest accepted webhook body limit, in bytes.</summary>
    public const int MinWebhookBytes = 1024;

    /// <summary>Largest accepted webhook body limit, in bytes.</summary>
    public const int MaxWebhookBytes = 1048576;

    /// <summary>Shortest accepted webhook retention, in days.</summary>
    public const int MinRetentionDays = 1;

    /// <summary>Longest accepted webhook retention, in days.</summary>
    public const int MaxRetentionDays = 365;

    /// <summary>Shortest accepted sweep interval, in hours.</summary>
    public const int MinSweepHours = 1;

    /// <summary>Longest accepted sweep interval, in hours.</summary>
    public const int MaxSweepHours = 24;
}
