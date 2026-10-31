namespace Core.Modules.Payments.Intents;

/// <summary>Static limits for a payment.</summary>
public static class PaymentConstant
{
    /// <summary>Length limits for free-text fields.</summary>
    public static class Constraints
    {
        /// <summary>External reference length.</summary>
        public const int ReferenceMaxLength = 128;

        /// <summary>Idempotency key length.</summary>
        public const int IdempotencyKeyMaxLength = 128;

        /// <summary>Failure code length.</summary>
        public const int FailureCodeMaxLength = 64;
    }

    /// <summary>HTTP header names used by the module.</summary>
    public static class Headers
    {
        /// <summary>Idempotency key header.</summary>
        public const string IdempotencyKey = "Idempotency-Key";

        /// <summary>Replay marker header.</summary>
        public const string IdempotentReplay = "Idempotent-Replay";

        /// <summary>Replay marker value.</summary>
        public const string ReplayTrue = "true";

        /// <summary>Fake webhook signature header.</summary>
        public const string FakeSignature = "X-Fake-Signature";

        /// <summary>Stripe webhook signature header.</summary>
        public const string StripeSignature = "Stripe-Signature";

        /// <summary>PayPal transmission id header.</summary>
        public const string PayPalTransmissionId = "PAYPAL-TRANSMISSION-ID";

        /// <summary>PayPal transmission time header.</summary>
        public const string PayPalTransmissionTime = "PAYPAL-TRANSMISSION-TIME";

        /// <summary>PayPal certificate URL header.</summary>
        public const string PayPalCertUrl = "PAYPAL-CERT-URL";

        /// <summary>PayPal auth algorithm header.</summary>
        public const string PayPalAuthAlgo = "PAYPAL-AUTH-ALGO";

        /// <summary>PayPal transmission signature header.</summary>
        public const string PayPalTransmissionSig = "PAYPAL-TRANSMISSION-SIG";
    }

    /// <summary>Content types accepted by the module.</summary>
    public static class ContentTypes
    {
        /// <summary>JSON content type.</summary>
        public const string Json = "application/json";
    }

    /// <summary>Canonical transition reasons stored on events.</summary>
    public static class Reasons
    {
        /// <summary>The customer cancelled.</summary>
        public const string CustomerCancelled = "customer_cancelled";

        /// <summary>The provider cancelled.</summary>
        public const string ProviderCancelled = "provider_cancelled";

        /// <summary>The provider reported a failure without a code.</summary>
        public const string ProviderFailed = "provider_failed";

        /// <summary>The provider rejected a capture without a code.</summary>
        public const string CaptureFailed = "capture_failed";
    }

    /// <summary>Canonical webhook processing outcomes.</summary>
    public static class Outcomes
    {
        /// <summary>Recorded but not yet processed.</summary>
        public const string Received = "Received";

        /// <summary>Applied to the aggregate.</summary>
        public const string Settled = "Settled";

        /// <summary>Ignored event type.</summary>
        public const string Ignored = "Ignored";

        /// <summary>Duplicate delivery.</summary>
        public const string Duplicate = "Duplicate";

        /// <summary>Amount or currency disagreed with the payment.</summary>
        public const string AmountMismatch = "AmountMismatch";

        /// <summary>Processing failed.</summary>
        public const string Failed = "Failed";
    }

    /// <summary>Constants used by the domain and adapter boundary.</summary>
    public static class Defaults
    {
        /// <summary>Reason recorded when a capture arrives after expiry.</summary>
        public const string LateSettlement = "LateSettlement";

        /// <summary>Reason recorded when settlement follows an earlier failure.</summary>
        public const string SettledAfterFailure = "SettledAfterFailure";

        /// <summary>Reason recorded for the first capture.</summary>
        public const string InitialCapture = "Initial";

        /// <summary>Default page size for payment queries.</summary>
        public const int DefaultPageSize = 20;

        /// <summary>Default maximum webhook body size in bytes.</summary>
        public const int WebhookMaxBytes = 65536;

        /// <summary>Default currency code.</summary>
        public const string Currency = "USD";
    }
}