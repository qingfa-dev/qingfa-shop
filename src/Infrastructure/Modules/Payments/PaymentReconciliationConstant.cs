namespace Infrastructure.Modules.Payments;

/// <summary>Limits and log identifiers for the reconciliation sweep.</summary>
public static class PaymentReconciliationConstant
{
    /// <summary>Shortest accepted sweep interval, in minutes.</summary>
    public const int MinIntervalMinutes = 1;

    /// <summary>Longest accepted sweep interval, in minutes.</summary>
    public const int MaxIntervalMinutes = 1440;

    /// <summary>Shortest accepted minimum age, in minutes.</summary>
    public const int MinAgeMinutes = 1;

    /// <summary>Longest accepted minimum age, in minutes.</summary>
    public const int MaxAgeMinutes = 1440;

    /// <summary>Smallest accepted batch.</summary>
    public const int MinBatchSize = 1;

    /// <summary>Largest accepted batch.</summary>
    public const int MaxBatchSize = 500;

    /// <summary>Stable log event identifiers.</summary>
    public static class LogEventId
    {
        /// <summary>Payments settled by a sweep.</summary>
        public const int Reconciled = 5201;

        /// <summary>One payment could not settle.</summary>
        public const int SettleFailed = 5202;

        /// <summary>The sweep failed.</summary>
        public const int SweepFailed = 5203;
    }
}
