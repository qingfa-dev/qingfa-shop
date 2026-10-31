using Core.BuildingBlocks.Functional.Errors;
using Core.Modules.Payments.Intents;

namespace Core.Modules.Payments;

/// <summary>Stable payment failure codes and their canonical errors.</summary>
/// <remarks>
/// Every failure the module can return is created here, so the code, message, and HTTP
/// status stay in one place. Handlers, endpoints, and adapters never build an <see cref="Error"/>.
/// </remarks>
public static class PaymentsOutcome
{
    /// <summary>Failure errors raised by the payment domain and its slices.</summary>
    public static class Failure
    {
        /// <summary>The payment does not exist for the caller.</summary>
        public static Error NotFound(Guid paymentId) => Error.NotFound(
            code: Codes.NotFound,
            message: $"Payment '{paymentId}' was not found.");

        /// <summary>A provider reference is not linked to any payment.</summary>
        public static Error ReferenceNotFound(string reference) => Error.NotFound(
            code: Codes.ReferenceNotFound,
            message: $"No payment matches the provider reference '{reference}'.");

        /// <summary>The requested transition is not allowed from the current state.</summary>
        public static Error InvalidState(PaymentStatus status, string action) => Error.Conflict(
            code: Codes.InvalidState,
            message: $"The payment is '{status.ToWire()}' and cannot be {action}.");

        /// <summary>The amount is outside the allowed range.</summary>
        public static Error AmountInvalid(string reason) => Error.BadRequest(
            code: Codes.AmountInvalid,
            message: $"Payment amount is invalid: {reason}.");

        /// <summary>The amount currency differs from the payment currency.</summary>
        public static Error CurrencyMismatch(string expected, string actual) => Error.BadRequest(
            code: Codes.CurrencyMismatch,
            message: $"The amount currency '{actual}' does not match the payment currency '{expected}'.");

        /// <summary>A required reference is missing.</summary>
        public static Error ReferenceRequired(string name) => Error.BadRequest(
            code: Codes.ReferenceRequired,
            message: $"Payment {name} is required.");

        /// <summary>A reference exceeds its limit.</summary>
        public static Error ReferenceTooLong(string name, int maxLength) => Error.BadRequest(
            code: Codes.ReferenceTooLong,
            message: $"Payment {name} must be at most {maxLength} characters.");

        /// <summary>The idempotency key header is missing.</summary>
        public static Error IdempotencyKeyRequired() => Error.BadRequest(
            code: Codes.IdempotencyKeyRequired,
            message: "The Idempotency-Key header is required.");

        /// <summary>An idempotency key exceeds its limit.</summary>
        public static Error IdempotencyKeyTooLong() => Error.BadRequest(
            code: Codes.IdempotencyKeyTooLong,
            message: $"The Idempotency-Key header must be at most {PaymentConstant.Constraints.IdempotencyKeyMaxLength} characters.");

        /// <summary>The key was reused with a different body.</summary>
        public static Error IdempotencyConflict() => Error.Conflict(
            code: Codes.IdempotencyConflict,
            message: "The idempotency key was reused with a different request.");

        /// <summary>The caller has no authenticated customer subject.</summary>
        public static Error CustomerRequired() => Error.Unauthorized(
            code: Codes.CustomerRequired,
            message: "An authenticated customer is required.");

        /// <summary>The payment has no provider reference yet.</summary>
        public static Error ProviderReferenceMissing() => Error.Conflict(
            code: Codes.ProviderReferenceMissing,
            message: "The payment has no provider reference yet.");

        /// <summary>The refund exceeds the capturable remainder.</summary>
        public static Error RefundExceedsCredit(decimal requested, decimal allowed) => Error.Conflict(
            code: Codes.RefundExceedsCredit,
            message: $"Refund of {requested} exceeds the allowed {allowed}.");

        /// <summary>The payment is not in a refundable state.</summary>
        public static Error RefundNotCaptured(PaymentStatus status) => Error.Conflict(
            code: Codes.RefundNotCaptured,
            message: $"The payment is '{status.ToWire()}' and cannot be refunded.");

        /// <summary>The refund does not exist for that payment.</summary>
        public static Error RefundNotFound(Guid refundId) => Error.NotFound(
            code: Codes.RefundNotFound,
            message: $"Refund '{refundId}' was not found.");

        /// <summary>The refund reason is missing or too long.</summary>
        public static Error RefundReasonRequired() => Error.BadRequest(
            code: Codes.RefundReasonRequired,
            message: $"A refund reason of at most {PaymentConstant.Constraints.ReasonMaxLength} characters is required.");

        /// <summary>The refund already reached a terminal state.</summary>
        public static Error RefundAlreadySettled(Guid refundId, string status) => Error.Conflict(
            code: Codes.RefundAlreadySettled,
            message: $"Refund '{refundId}' is already '{status}' and cannot change.");

        /// <summary>The provider cannot refund part of a capture.</summary>
        public static Error PartialRefundUnsupported() => Error.BadRequest(
            code: Codes.PartialRefundUnsupported,
            message: "This provider does not support partial refunds.");

        /// <summary>The provider name is unknown.</summary>
        public static Error ProviderUnknown(string provider) => Error.BadRequest(
            code: Codes.ProviderUnknown,
            message: $"The '{provider}' provider is unknown.");

        /// <summary>The capture mode is unknown.</summary>
        public static Error CaptureModeInvalid(string captureMode) => Error.BadRequest(
            code: Codes.CaptureModeInvalid,
            message: $"The '{captureMode}' capture mode is unknown.");

        /// <summary>The provider is not enabled or configured.</summary>
        public static Error ProviderDisabled(PaymentProvider provider) => Error.ServiceUnavailable(
            code: Codes.ProviderDisabled,
            message: $"The '{provider.ToWire()}' provider is not enabled.");

        /// <summary>The provider rejected the request.</summary>
        public static Error ProviderRequestRejected(PaymentProvider provider) => Error.Custom(
            code: Codes.ProviderRequestRejected,
            message: $"The '{provider.ToWire()}' provider rejected the request.",
            status: 502);

        /// <summary>The provider is unreachable or timed out.</summary>
        public static Error ProviderUnavailable(PaymentProvider provider) => Error.Custom(
            code: Codes.ProviderUnavailable,
            message: $"The '{provider.ToWire()}' provider is unavailable.",
            status: 502);

        /// <summary>The provider declined a capture or refund.</summary>
        public static Error ProviderDeclined(string? providerCode) => Error.Custom(
            code: Codes.ProviderDeclined,
            message: $"The provider declined the operation ({providerCode ?? "unknown"}).",
            status: 502);

        /// <summary>The provider rejected the credentials.</summary>
        public static Error ProviderConfigurationInvalid(PaymentProvider provider) => Error.InternalServerError(
            code: Codes.ProviderConfigurationInvalid,
            message: $"The '{provider.ToWire()}' provider configuration is invalid.");

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
        // Payment

        /// <summary>Payment not found.</summary>
        public const string NotFound = "Payments.Payment.NotFound";

        /// <summary>Provider reference not linked to a payment.</summary>
        public const string ReferenceNotFound = "Payments.Payment.ReferenceNotFound";

        /// <summary>Illegal transition.</summary>
        public const string InvalidState = "Payments.Payment.InvalidState";

        /// <summary>Invalid amount.</summary>
        public const string AmountInvalid = "Payments.Payment.AmountInvalid";

        /// <summary>Amount currency differs from the payment currency.</summary>
        public const string CurrencyMismatch = "Payments.Payment.CurrencyMismatch";

        /// <summary>Missing external reference.</summary>
        public const string ReferenceRequired = "Payments.Payment.ReferenceRequired";

        /// <summary>Reference exceeds its limit.</summary>
        public const string ReferenceTooLong = "Payments.Payment.ReferenceTooLong";

        /// <summary>Missing idempotency key.</summary>
        public const string IdempotencyKeyRequired = "Payments.Payment.IdempotencyKeyRequired";

        /// <summary>Idempotency key exceeds its limit.</summary>
        public const string IdempotencyKeyTooLong = "Payments.Payment.IdempotencyKeyTooLong";

        /// <summary>Idempotency key reuse with a different body.</summary>
        public const string IdempotencyConflict = "Payments.Payment.IdempotencyConflict";

        /// <summary>Unknown capture mode.</summary>
        public const string CaptureModeInvalid = "Payments.CaptureMode.Invalid";

        // Customer

        /// <summary>Missing authenticated customer.</summary>
        public const string CustomerRequired = "Payments.Customer.Required";

        // Refund

        /// <summary>Refund larger than the captured remainder.</summary>
        public const string RefundExceedsCredit = "Payments.Refund.ExceedsCredit";

        /// <summary>Refund against a payment that is not captured.</summary>
        public const string RefundNotCaptured = "Payments.Refund.NotCaptured";

        /// <summary>Unknown refund.</summary>
        public const string RefundNotFound = "Payments.Refund.NotFound";

        /// <summary>Missing or over-long refund reason.</summary>
        public const string RefundReasonRequired = "Payments.Refund.ReasonRequired";

        /// <summary>Refund already settled.</summary>
        public const string RefundAlreadySettled = "Payments.Refund.AlreadySettled";

        /// <summary>Partial refund not supported by the provider.</summary>
        public const string PartialRefundUnsupported = "Payments.Refund.PartialUnsupported";

        // Provider

        /// <summary>Unknown provider name.</summary>
        public const string ProviderUnknown = "Payments.Provider.Unknown";

        /// <summary>Provider call before a reference existed.</summary>
        public const string ProviderReferenceMissing = "Payments.Provider.ReferenceMissing";

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

        // Webhook

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
