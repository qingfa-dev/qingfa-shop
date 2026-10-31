namespace Application.Modules.Payments.Persistence;

/// <summary>Whether an idempotency reservation is new or a replay.</summary>
/// <param name="PaymentId">The reserved or existing payment id.</param>
/// <param name="IsNew">True when this call created the reservation.</param>
/// <param name="FingerprintMatches">True when a replay matches the original request.</param>
public sealed record IdempotencyReservation(Guid PaymentId, bool IsNew, bool FingerprintMatches);

/// <summary>Reserves idempotency keys synchronously, independent of projections.</summary>
public interface IPaymentIdempotencyStore
{
    /// <summary>Reserves a key or returns the existing reservation.</summary>
    /// <param name="customerId">The owning customer.</param>
    /// <param name="idempotencyKey">The caller's key.</param>
    /// <param name="fingerprint">The request fingerprint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reservation, or a stable failure.</returns>
    Task<Result<IdempotencyReservation>> ReserveAsync(
        Guid customerId,
        string idempotencyKey,
        string fingerprint,
        CancellationToken cancellationToken);
}
