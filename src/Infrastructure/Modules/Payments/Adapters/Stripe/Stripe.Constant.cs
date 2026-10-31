namespace Infrastructure.Modules.Payments.Adapters.Stripe;

/// <summary>Stripe API vocabulary the adapter maps onto the canonical model.</summary>
public static class StripeConstant
{
    /// <summary>Metadata keys written on a Stripe intent.</summary>
    public static class Metadata
    {
        /// <summary>The internal payment id.</summary>
        public const string PaymentId = "payment_id";

        /// <summary>The caller's external reference.</summary>
        public const string ExternalReference = "external_reference";
    }

    /// <summary>Intent statuses read from the Stripe API.</summary>
    public static class IntentStatus
    {
        /// <summary>The customer must act.</summary>
        public const string RequiresAction = "requires_action";

        /// <summary>The intent waits for confirmation.</summary>
        public const string RequiresConfirmation = "requires_confirmation";

        /// <summary>A payment method is still required.</summary>
        public const string RequiresPaymentMethod = "requires_payment_method";

        /// <summary>Funds are held and capturable.</summary>
        public const string RequiresCapture = "requires_capture";

        /// <summary>The provider is processing.</summary>
        public const string Processing = "processing";

        /// <summary>Funds were captured.</summary>
        public const string Succeeded = "succeeded";

        /// <summary>The intent was cancelled.</summary>
        public const string Canceled = "canceled";
    }

    /// <summary>Event types the adapter acts on.</summary>
    public static class EventType
    {
        /// <summary>Funds became capturable.</summary>
        public const string AmountCapturable = "payment_intent.amount_capturable_updated";

        /// <summary>The intent succeeded.</summary>
        public const string Succeeded = "payment_intent.succeeded";

        /// <summary>The payment failed.</summary>
        public const string PaymentFailed = "payment_intent.payment_failed";

        /// <summary>The intent was cancelled.</summary>
        public const string Canceled = "payment_intent.canceled";

        /// <summary>A charge was refunded.</summary>
        public const string ChargeRefunded = "charge.refunded";

        /// <summary>A refund changed state.</summary>
        public const string ChargeRefundUpdated = "charge.refund.updated";
    }

    /// <summary>How long a created intent claims to stay payable.</summary>
    public const int IntentLifetimeMinutes = 30;

    /// <summary>Capture methods sent to Stripe.</summary>
    public static class CaptureMethod
    {
        /// <summary>Authorize and capture immediately.</summary>
        public const string Automatic = "automatic";

        /// <summary>Authorize now, capture later.</summary>
        public const string Manual = "manual";
    }
}
