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

    /// <summary>Capture methods sent to Stripe.</summary>
    public static class CaptureMethod
    {
        /// <summary>Authorize and capture immediately.</summary>
        public const string Automatic = "automatic";

        /// <summary>Authorize now, capture later.</summary>
        public const string Manual = "manual";
    }
}
