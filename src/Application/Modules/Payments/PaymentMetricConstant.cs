namespace Application.Modules.Payments;

/// <summary>Names and tags emitted by the payments meter.</summary>
public static class PaymentMetricConstant
{
    /// <summary>The meter name registered with OpenTelemetry.</summary>
    public const string MeterName = "QingFa.Shop.Payments";

    /// <summary>The provider tag name.</summary>
    public const string ProviderTag = "provider";

    /// <summary>The operation tag name.</summary>
    public const string OperationTag = "operation";

    /// <summary>The latency histogram unit.</summary>
    public const string MillisecondsUnit = "ms";

    /// <summary>Instrument names emitted by the meter.</summary>
    public static class Instrument
    {
        /// <summary>Payments created.</summary>
        public const string Initiated = "payments.initiated";

        /// <summary>Settlements applied.</summary>
        public const string Settled = "payments.settled";

        /// <summary>Payments that failed.</summary>
        public const string Failed = "payments.failed";

        /// <summary>Refunds that succeeded.</summary>
        public const string RefundsSucceeded = "refunds.succeeded";

        /// <summary>Webhook deliveries accepted.</summary>
        public const string WebhooksReceived = "webhooks.received";

        /// <summary>Webhook deliveries deduplicated.</summary>
        public const string WebhooksDuplicate = "webhooks.duplicate";

        /// <summary>Webhook deliveries rejected for amount mismatch.</summary>
        public const string WebhooksMismatch = "webhooks.mismatch";

        /// <summary>Webhook deliveries pruned.</summary>
        public const string WebhooksPruned = "webhooks.pruned";

        /// <summary>Payments settled by the reconciliation sweep.</summary>
        public const string Reconciled = "payments.reconciled";

        /// <summary>Provider call latency.</summary>
        public const string ProviderDuration = "payments.provider.duration";
    }
}
