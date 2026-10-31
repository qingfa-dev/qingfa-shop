using Application.Modules.Payments.Ports.Models;

using Core.BuildingBlocks.Functional.Maybes;
using Core.Modules.Payments;

namespace Application.Modules.Payments.Ports;

/// <summary>
/// Defines provider-neutral payment operations.
/// Infrastructure adapters translate these operations into provider-specific APIs.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>The provider implemented by this gateway.</summary>
    PaymentProvider Provider { get; }

    /// <summary>Operations and features supported by this gateway.</summary>
    PaymentGatewayCapabilities Capabilities { get; }

    /// <summary>Creates a provider-side payment intent.</summary>
    /// <param name="request">The canonical intent request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The provider intent, or a stable failure.</returns>
    Task<Result<PaymentIntentResult>> CreateIntentAsync(
        PaymentIntentRequest request,
        CancellationToken cancellationToken);

    /// <summary>Retrieves the current provider-side state of a payment intent.</summary>
    /// <param name="providerReference">The provider reference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current intent state, or a stable failure.</returns>
    Task<Result<PaymentIntentResult>> RetrieveAsync(
        string providerReference,
        CancellationToken cancellationToken);

    /// <summary>Captures previously authorized funds.</summary>
    /// <param name="request">The canonical capture request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The capture result, or a stable failure.</returns>
    Task<Result<PaymentCaptureResult>> CaptureAsync(
        PaymentCaptureRequest request,
        CancellationToken cancellationToken);

    /// <summary>Cancels an intent or voids an uncaptured authorization.</summary>
    /// <param name="request">The canonical cancel request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cancel result, or a stable failure.</returns>
    Task<Result<PaymentCancelResult>> CancelAsync(
        PaymentCancelRequest request,
        CancellationToken cancellationToken);

    /// <summary>Refunds previously captured funds.</summary>
    /// <param name="request">The canonical refund request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The refund result, or a stable failure.</returns>
    Task<Result<PaymentRefundResult>> RefundAsync(
        PaymentRefundRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verifies and normalizes a provider webhook.
    /// Returns an empty Maybe when the event should be ignored.
    /// </summary>
    /// <param name="rawBody">The exact request bytes; signatures are verified over them.</param>
    /// <param name="headers">The request headers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The normalized event, an empty Maybe, or a stable failure.</returns>
    Task<Result<Maybe<PaymentProviderEvent>>> ParseWebhookAsync(
        ReadOnlyMemory<byte> rawBody,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken);
}
