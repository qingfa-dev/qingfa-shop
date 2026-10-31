namespace Infrastructure.Payments.Configuration;

/// <summary>Settings for the provider reconciliation sweep.</summary>
public sealed class PaymentReconciliationOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Payments:Reconciliation";

    /// <summary>Default minutes between sweeps.</summary>
    public const int DefaultIntervalMinutes = 15;

    /// <summary>Default age, in minutes, before a payment is reconciled.</summary>
    public const int DefaultMinimumAgeMinutes = 10;

    /// <summary>Default number of payments examined per sweep.</summary>
    public const int DefaultBatchSize = 50;

    /// <summary>Whether the reconciliation worker runs.</summary>
    public bool Enabled { get; set; }

    /// <summary>How often the sweep runs, in minutes.</summary>
    public int IntervalMinutes { get; set; } = DefaultIntervalMinutes;

    /// <summary>How old a pending payment must be before it is reconciled, in minutes.</summary>
    public int MinimumAgeMinutes { get; set; } = DefaultMinimumAgeMinutes;

    /// <summary>How many payments one sweep examines.</summary>
    public int BatchSize { get; set; } = DefaultBatchSize;
}
