namespace Infrastructure.Modules.Payments.Adapters.PayPal;

/// <summary>PayPal API vocabulary the adapter maps onto the canonical model.</summary>
public static class PayPalConstant
{
    /// <summary>Request headers the adapter sends and reads.</summary>
    public static class Headers
    {
        /// <summary>The OAuth authorization scheme.</summary>
        public const string AuthorizationScheme = "Bearer";

        /// <summary>The idempotency header.</summary>
        public const string RequestId = "PayPal-Request-Id";
    }

    /// <summary>Order statuses read from the Orders API.</summary>
    public static class OrderStatus
    {
        /// <summary>The order is created and awaiting approval.</summary>
        public const string Created = "CREATED";

        /// <summary>The payer approved the order.</summary>
        public const string Approved = "APPROVED";

        /// <summary>The order completed.</summary>
        public const string Completed = "COMPLETED";
    }

    /// <summary>Event types the adapter acts on.</summary>
    public static class EventType
    {
        /// <summary>An order was approved.</summary>
        public const string OrderApproved = "CHECKOUT.ORDER.APPROVED";

        /// <summary>A capture completed.</summary>
        public const string CaptureCompleted = "PAYMENT.CAPTURE.COMPLETED";

        /// <summary>A capture was denied.</summary>
        public const string CaptureDenied = "PAYMENT.CAPTURE.DENIED";

        /// <summary>A capture was refunded.</summary>
        public const string CaptureRefunded = "PAYMENT.CAPTURE.REFUNDED";

        /// <summary>A customer opened a dispute.</summary>
        public const string DisputeCreated = "CUSTOMER.DISPUTE.CREATED";

        /// <summary>A customer dispute was resolved.</summary>
        public const string DisputeResolved = "CUSTOMER.DISPUTE.RESOLVED";
    }

    /// <summary>Values returned by the webhook verification endpoint.</summary>
    public static class Verification
    {
        /// <summary>The signature verified.</summary>
        public const string Success = "SUCCESS";

        /// <summary>The verification status property.</summary>
        public const string StatusProperty = "verification_status";
    }

    /// <summary>Order intents sent to PayPal.</summary>
    public static class OrderIntent
    {
        /// <summary>Capture immediately.</summary>
        public const string Capture = "CAPTURE";

        /// <summary>Authorize now, capture later.</summary>
        public const string Authorize = "AUTHORIZE";
    }
}
