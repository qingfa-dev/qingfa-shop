namespace Core.Modules.Payments.Intents;

/// <summary>Static limits and canonical values for the payments module.</summary>
public static class PaymentConstant
{
    /// <summary>Length limits mirrored by the persistence mappings.</summary>
    public static class Constraints
    {
        /// <summary>Provider name length.</summary>
        public const int ProviderMaxLength = 16;

        /// <summary>Status length.</summary>
        public const int StatusMaxLength = 32;

        /// <summary>Currency code length.</summary>
        public const int CurrencyMaxLength = 3;

        /// <summary>External reference length.</summary>
        public const int ReferenceMaxLength = 128;

        /// <summary>Idempotency key length.</summary>
        public const int IdempotencyKeyMaxLength = 128;

        /// <summary>Provider reference length.</summary>
        public const int ProviderReferenceMaxLength = 128;

        /// <summary>Provider event id length.</summary>
        public const int ProviderEventIdMaxLength = 128;

        /// <summary>Provider event type length.</summary>
        public const int EventTypeMaxLength = 64;

        /// <summary>Failure code length.</summary>
        public const int FailureCodeMaxLength = 64;

        /// <summary>Failure message length.</summary>
        public const int FailureMessageMaxLength = 512;

        /// <summary>Refund reason length.</summary>
        public const int ReasonMaxLength = 256;

        /// <summary>Largest page size a query may request.</summary>
        public const int MaxPageSize = 100;
    }

    /// <summary>Paging helpers so every payment query bounds the page size.</summary>
    public static class Paging
    {
        /// <summary>Clamps a requested page size into the allowed range.</summary>
        /// <param name="pageSize">The requested size, if any.</param>
        /// <returns>A size between 1 and <see cref="Constraints.MaxPageSize"/>.</returns>
        public static int ClampSize(int? pageSize) => pageSize switch
        {
            null or < 1 => Defaults.DefaultPageSize,
            > Constraints.MaxPageSize => Constraints.MaxPageSize,
            _ => pageSize.Value
        };
    }

    /// <summary>HTTP status codes used by the outcome catalogue.</summary>
    public static class HttpStatus
    {
        /// <summary>400 Bad Request.</summary>
        public const int BadRequest = 400;

        /// <summary>401 Unauthorized.</summary>
        public const int Unauthorized = 401;

        /// <summary>404 Not Found.</summary>
        public const int NotFound = 404;

        /// <summary>409 Conflict.</summary>
        public const int Conflict = 409;

        /// <summary>413 Content Too Large.</summary>
        public const int ContentTooLarge = 413;

        /// <summary>415 Unsupported Media Type.</summary>
        public const int UnsupportedMediaType = 415;

        /// <summary>500 Internal Server Error.</summary>
        public const int InternalServerError = 500;

        /// <summary>502 Bad Gateway.</summary>
        public const int BadGateway = 502;

        /// <summary>503 Service Unavailable.</summary>
        public const int ServiceUnavailable = 503;
    }

    /// <summary>Response metadata keys.</summary>
    public static class Metadata
    {
        /// <summary>Marks a replayed idempotent request.</summary>
        public const string IdempotencyReplay = "IdempotentReplay";
    }

    /// <summary>Deterministic idempotency key prefixes used towards providers.</summary>
    public static class IdempotencyPrefixes
    {
        /// <summary>Create prefix; the caller's key follows it.</summary>
        public const string Create = "create:";

        /// <summary>Confirm prefix.</summary>
        public const string Confirm = "confirm:";

        /// <summary>Capture prefix.</summary>
        public const string Capture = "capture:";

        /// <summary>Cancel prefix.</summary>
        public const string Cancel = "cancel:";

        /// <summary>Refund prefix.</summary>
        public const string Refund = "refund:";
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

    /// <summary>Canonical reasons recorded on payment and refund events.</summary>
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

        /// <summary>The provider rejected a refund without a code.</summary>
        public const string RefundFailed = "refund_failed";

        /// <summary>Recorded when a capture arrives after expiry.</summary>
        public const string LateSettlement = "late_settlement";

        /// <summary>Recorded when settlement follows an earlier failure.</summary>
        public const string SettledAfterFailure = "settled_after_failure";

        /// <summary>Recorded for the first capture.</summary>
        public const string InitialCapture = "initial_capture";
    }

    /// <summary>Canonical webhook processing outcomes.</summary>
    public static class Outcomes
    {
        /// <summary>Recorded but not yet processed.</summary>
        public const string Received = "Received";

        /// <summary>Applied to the aggregate.</summary>
        public const string Settled = "Settled";

        /// <summary>Amount or currency disagreed with the payment.</summary>
        public const string AmountMismatch = "AmountMismatch";

        /// <summary>Processing failed.</summary>
        public const string Failed = "Failed";
    }

    /// <summary>Default values used at the module boundary.</summary>
    public static class Defaults
    {
        /// <summary>Default page size for payment queries.</summary>
        public const int DefaultPageSize = 20;

        /// <summary>Default currency code.</summary>
        public const string Currency = "USD";

        /// <summary>Default ceiling for a single payment.</summary>
        public const decimal MaxAmount = 100000m;
    }
}
