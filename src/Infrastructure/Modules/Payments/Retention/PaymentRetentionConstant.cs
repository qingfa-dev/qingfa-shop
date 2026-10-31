namespace Infrastructure.Modules.Payments.Retention;

/// <summary>Values the webhook retention worker logs and uses.</summary>
public static class PaymentRetentionConstant
{
    /// <summary>Stable log event identifiers.</summary>
    public static class EventId
    {
        /// <summary>A sweep removed rows.</summary>
        public const int Pruned = 5101;

        /// <summary>A sweep failed.</summary>
        public const int SweepFailed = 5102;
    }

    /// <summary>Legacy log message fragments kept for operators.</summary>
    public static class Message
    {
        /// <summary>Prefix used when a sweep fails.</summary>
        public const string SweepFailed = "Webhook retention sweep failed";
    }
}
