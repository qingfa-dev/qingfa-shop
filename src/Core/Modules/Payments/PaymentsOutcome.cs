using Core.BuildingBlocks.Functional.Errors;
using Core.Modules.Payments.Intents;

namespace Core.Modules.Payments;

/// <summary>Stable payment failure codes and their canonical errors.</summary>
public static class PaymentsOutcome
{
    /// <summary>Failure errors raised by the payment domain and its slices.</summary>
    public static class Failure
    {
        /// <summary>The payment does not exist for the caller.</summary>
        public static Error NotFound(Guid paymentId) => Error.NotFound(
            code: Codes.NotFound,
            message: $"Payment '{paymentId}' was not found.");

        /// <summary>A provider reference is unknown.</summary>
        public static Error ReferenceNotFound(string reference) => Error.NotFound(
            code: Codes.NotFound,
            message: $"No payment matches the provider reference '{reference}'.");

        /// <summary>The requested transition is not allowed from the current state.</summary>
        public static Error InvalidState(PaymentStatus status, string operation) => Error.Conflict(
            code: Codes.InvalidState,
            message: $"A payment cannot {operation} while it is '{status.ToWire()}'.");

        /// <summary>The amount or currency is invalid.</summary>
        public static Error AmountInvalid(string reason) => Error.BadRequest(
            code: Codes.AmountInvalid,
            message: $"Payment amount is invalid: {reason}.");

        /// <summary>The currency has no supported exponent.</summary>
        public static Error CurrencyUnsupported(string currency) => Error.BadRequest(
            code: Codes.CurrencyUnsupported,
            message: $"The currency '{currency}' is not supported.");

        /// <summary>A reference exceeds its limit.</summary>
        public static Error ReferenceTooLong(string name) => Error.BadRequest(
            code: Codes.ReferenceTooLong,
            message: $"Payment {name} is too long.");

        /// <summary>A required reference is missing.</summary>
        public static Error ReferenceRequired(string name) => Error.BadRequest(
            code: Codes.ReferenceRequired,
            message: $"Payment {name} is required.");

        /// <summary>An idempotency key exceeds its limit.</summary>
        public static Error IdempotencyKeyTooLong() => Error.BadRequest(
            code: Codes.IdempotencyKeyTooLong,
            message: "The Idempotency-Key header is too long.");

        /// <summary>The amount currency differs from the payment currency.</summary>
        public static Error CurrencyMismatch(string expected, string actual) => Error.BadRequest(
            code: Codes.CurrencyMismatch,
            message: $"The amount currency '{actual}' does not match the payment currency '{expected}'.");

        /// <summary>A customer action targeted an expired intent.</summary>
        public static Error Expired() => Error.Conflict(
            code: Codes.Expired,
            message: "The payment intent has expired.");

        /// <summary>The refund reason is missing or too long.</summary>
        public static Error RefundReasonRequired() => Error.BadRequest(
            code: Codes.RefundReasonRequired,
            message: "A refund reason of at most 256 characters is required.");

        /// <summary>The refund already succeeded and cannot change.</summary>
        public static Error RefundAlreadySettled(Guid refundId) => Error.Conflict(
            code: Codes.RefundAlreadySettled,
            message: $"Refund '{refundId}' has already settled.");

        /// <summary>A provider call requires a reference that does not exist yet.</summary>
        public static Error ProviderReferenceMissing() => Error.Conflict(
            code: Codes.ProviderReferenceMissing,
            message: "The payment has no provider reference yet.");

        /// <summary>The idempotency key header is missing.</summary>
        public static Error IdempotencyKeyRequired() => Error.BadRequest(
            code: Codes.IdempotencyKeyRequired,
            message: "The Idempotency-Key header is required.");

        /// <summary>The key was reused with a different body.</summary>
        public static Error IdempotencyConflict() => Error.Conflict(
            code: Codes.IdempotencyConflict,
            message: "The idempotency key was reused with a different request.");

        /// <summary>The caller has no authenticated customer subject.</summary>
        public static Error CustomerRequired() => Error.Unauthorized(
            code: Codes.CustomerRequired,
            message: "An authenticated customer is required.");

        /// <summary>The refund exceeds the capturable remainder.</summary>
        public static Error RefundExceedsCredit(decimal requested, decimal allowed) => Error.Conflict(
            code: Codes.RefundExceedsCredit,
            message: $"Refund of {requested} exceeds the allowed {allowed}.");

        /// <summary>The payment is not in a refundable state.</summary>
        public static Error RefundNotCaptured(PaymentStatus status) => Error.Conflict(
            code: Codes.RefundNotCaptured,
            message: $"A payment in '{status}' cannot be refunded.");

        /// <summary>The refund does not exist for that payment.</summary>
        public static Error RefundNotFound(Guid refundId) => Error.NotFound(
            code: Codes.RefundNotFound,
            message: $"Refund '{refundId}' was not found.");

        /// <summary>The provider cannot refund part of a capture.</summary>
        public static Error PartialRefundUnsupported() => Error.BadRequest(
            code: Codes.PartialRefundUnsupported,
            message: "This provider does not support partial refunds.");

        /// <summary>The provider is not enabled or configured.</summary>
        public static Error ProviderDisabled(PaymentProvider provider) => Error.ServiceUnavailable(
            code: Codes.ProviderDisabled,
            message: $"The '{provider}' provider is not enabled.");

        /// <summary>The provider rejected the request.</summary>
        public static Error ProviderRequestRejected(PaymentProvider provider) => Error.Custom(
            code: Codes.ProviderRequestRejected,
            message: $"The '{provider}' provider rejected the request.",
            status: 502);

        /// <summary>The provider is unreachable or timed out.</summary>
        public static Error ProviderUnavailable(PaymentProvider provider) => Error.Custom(
            code: Codes.ProviderUnavailable,
            message: $"The '{provider}' provider is unavailable.",
            status: 502);

        /// <summary>The provider declined a capture or refund.</summary>
        public static Error ProviderDeclined(string? providerCode) => Error.Custom(
            code: Codes.ProviderDeclined,
            message: $"The provider declined the operation ({providerCode ?? "unknown"}).",
            status: 502);

        /// <summary>The provider rejected the credentials.</summary>
        public static Error ProviderConfigurationInvalid(PaymentProvider provider) => Error.InternalServerError(
            code: Codes.ProviderConfigurationInvalid,
            message: $"The '{provider}' provider configuration is invalid.");

        /// <summary>The webhook signature is missing or invalid.</summary>
        public static Error WebhookSignatureInvalid() => Error.BadRequest(
            code: Codes.WebhookSignatureInvalid,
            message: "The webhook signature is invalid.");

        /// <summary>The webhook body is not JSON.</summary>
        public static Error WebhookContentTypeUnsupported() => Error.Custom(
            code: Codes.WebhookContentTypeUnsupported,
            message: "The webhook body must be application/json.",
            status: 415);

        /// <summary>The webhook body cannot be parsed.</summary>
        public static Error WebhookPayloadInvalid() => Error.BadRequest(
            code: Codes.WebhookPayloadInvalid,
            message: "The webhook payload is invalid.");

        /// <summary>The webhook body exceeds the configured limit.</summary>
        public static Error WebhookPayloadTooLarge() => Error.Custom(
            code: Codes.WebhookPayloadTooLarge,
            message: "The webhook body is too large.",
            status: 413);

        /// <summary>The provider segment is unknown.</summary>
        public static Error WebhookProviderUnsupported(string provider) => Error.NotFound(
            code: Codes.WebhookProviderUnsupported,
            message: $"The '{provider}' provider is unknown.");
    }

    /// <summary>Stable error codes for contracts and tests.</summary>
    public static class Codes
    {
        /// <summary>Payment not found.</summary>
        public const string NotFound = "Payments.Payment.NotFound";

        /// <summary>Illegal transition.</summary>
        public const string InvalidState = "Payments.Payment.InvalidState";

        /// <summary>Invalid amount.</summary>
        public const string AmountInvalid = "Payments.Payment.AmountInvalid";

        /// <summary>Unsupported currency exponent.</summary>
        public const string CurrencyUnsupported = "Payments.Payment.CurrencyUnsupported";

        /// <summary>Missing external reference.</summary>
        public const string ReferenceRequired = "Payments.Payment.ReferenceRequired";

        /// <summary>Reference exceeds its limit.</summary>
        public const string ReferenceTooLong = "Payments.Payment.ReferenceTooLong";

        /// <summary>Idempotency key exceeds its limit.</summary>
        public const string IdempotencyKeyTooLong = "Payments.Payment.IdempotencyKeyTooLong";

        /// <summary>Amount currency differs from the payment currency.</summary>
        public const string CurrencyMismatch = "Payments.Payment.CurrencyMismatch";

        /// <summary>Customer action on an expired intent.</summary>
        public const string Expired = "Payments.Payment.Expired";

        /// <summary>Missing or over-long refund reason.</summary>
        public const string RefundReasonRequired = "Payments.Refund.ReasonRequired";

        /// <summary>Refund already settled.</summary>
        public const string RefundAlreadySettled = "Payments.Refund.AlreadySettled";

        /// <summary>Provider call before a reference existed.</summary>
        public const string ProviderReferenceMissing = "Payments.Provider.ReferenceMissing";

        /// <summary>Missing idempotency key.</summary>
        public const string IdempotencyKeyRequired = "Payments.Payment.IdempotencyKeyRequired";

        /// <summary>Idempotency key reuse with a different body.</summary>
        public const string IdempotencyConflict = "Payments.Payment.IdempotencyConflict";

        /// <summary>Missing authenticated customer.</summary>
        public const string CustomerRequired = "Payments.Customer.Required";

        /// <summary>Refund larger than the captured remainder.</summary>
        public const string RefundExceedsCredit = "Payments.Refund.ExceedsCredit";

        /// <summary>Refund against a payment that is not captured.</summary>
        public const string RefundNotCaptured = "Payments.Refund.NotCaptured";

        /// <summary>Unknown refund.</summary>
        public const string RefundNotFound = "Payments.Refund.NotFound";

        /// <summary>Partial refund not supported by the provider.</summary>
        public const string PartialRefundUnsupported = "Payments.Refund.PartialUnsupported";

        /// <summary>Provider not enabled.</summary>
        public const string ProviderDisabled = "Payments.Provider.Disabled";

        /// <summary>Provider rejected the request.</summary>
        public const string ProviderRequestRejected = "Payments.Provider.RequestRejected";

        /// <summary>Provider unreachable.</summary>
        public const string ProviderUnavailable = "Payments.Provider.Unavailable";

        /// <summary>Provider declined.</summary>
        public const string ProviderDeclined = "Payments.Provider.Declined";

        /// <summary>Provider credentials rejected.</summary>
        public const string ProviderConfigurationInvalid = "Payments.Provider.ConfigurationInvalid";

        /// <summary>Bad webhook signature.</summary>
        public const string WebhookSignatureInvalid = "Payments.Webhook.SignatureInvalid";

        /// <summary>Unsupported webhook content type.</summary>
        public const string WebhookContentTypeUnsupported = "Payments.Webhook.ContentTypeUnsupported";

        /// <summary>Unparseable webhook payload.</summary>
        public const string WebhookPayloadInvalid = "Payments.Webhook.PayloadInvalid";

        /// <summary>Oversized webhook body.</summary>
        public const string WebhookPayloadTooLarge = "Payments.Webhook.PayloadTooLarge";

        /// <summary>Unknown webhook provider.</summary>
        public const string WebhookProviderUnsupported = "Payments.Webhook.ProviderUnsupported";

        /// <summary>Amount mismatch outcome recorded on the webhook row.</summary>
        public const string WebhookAmountMismatch = "Payments.Webhook.AmountMismatch";
    }
}