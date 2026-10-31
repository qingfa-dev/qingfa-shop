using Core.Modules.Payments;

namespace Application.Modules.Payments.Persistence;

/// <summary>Maps provider references to payment ids without waiting for projections.</summary>
public interface IPaymentReferenceStore
{
    /// <summary>Records a provider reference for a payment.</summary>
    /// <param name="provider">The provider.</param>
    /// <param name="providerReference">The provider reference.</param>
    /// <param name="paymentId">The payment id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or a stable failure.</returns>
    Task<Result> RecordAsync(
        PaymentProvider provider,
        string providerReference,
        Guid paymentId,
        CancellationToken cancellationToken);

    /// <summary>Finds the payment id for a provider reference.</summary>
    /// <param name="provider">The provider.</param>
    /// <param name="providerReference">The provider reference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The payment id, or a not-found failure.</returns>
    Task<Result<Guid>> FindAsync(
        PaymentProvider provider,
        string providerReference,
        CancellationToken cancellationToken);
}
