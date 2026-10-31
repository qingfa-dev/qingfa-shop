using Application.Modules.Payments.Ports.Models;

using Core.BuildingBlocks.Functional.Maybes;
using Core.Modules.Payments;

namespace Application.Modules.Payments.Ports;

/// <summary>Provider-neutral payment operations.</summary>
public interface IPaymentGateway
{
    /// <summary>The provider this adapter implements.</summary>
    PaymentProvider Provider { get; }

    /// <summary>Creates a provider intent.</summary>
    Task<Result<PaymentIntentResult>> CreateIntentAsync(PaymentIntentRequest request, CancellationToken cancellationToken);

    /// <summary>Reads the current provider state of an intent.</summary>
    Task<Result<PaymentIntentResult>> RetrieveAsync(string providerReference, CancellationToken cancellationToken);

    /// <summary>Captures authorized funds.</summary>
    Task<Result<PaymentCaptureResult>> CaptureAsync(PaymentCaptureRequest request, CancellationToken cancellationToken);

    /// <summary>Voids an uncaptured authorization.</summary>
    Task<Result<PaymentCancelResult>> CancelAsync(PaymentCancelRequest request, CancellationToken cancellationToken);

    /// <summary>Refunds captured funds.</summary>
    Task<Result<PaymentRefundResult>> RefundAsync(PaymentRefundRequest request, CancellationToken cancellationToken);

    /// <summary>Verifies and normalizes a webhook; returns an empty Maybe for ignored events.</summary>
    Task<Result<Maybe<PaymentProviderEvent>>> ParseWebhookAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken);
}
