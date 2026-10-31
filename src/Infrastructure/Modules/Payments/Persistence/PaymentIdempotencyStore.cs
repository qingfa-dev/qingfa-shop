using Application.Modules.Payments.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Payments.Persistence;

/// <summary>Reserves idempotency keys in the database.</summary>
public sealed class PaymentIdempotencyStore(DbContext context, TimeProvider clock) : IPaymentIdempotencyStore
{
    /// <inheritdoc />
    public async Task<Result<IdempotencyReservation>> ReserveAsync(
        Guid customerId,
        string idempotencyKey,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        PaymentIdempotencyKey? existing = await FindAsync(customerId, idempotencyKey, cancellationToken);

        if (existing is not null)
            return Result<IdempotencyReservation>.Success(new IdempotencyReservation(
                existing.PaymentId, IsNew: false, FingerprintMatches: existing.Fingerprint == fingerprint));

        Guid paymentId = Guid.NewGuid();

        context.Set<PaymentIdempotencyKey>().Add(new PaymentIdempotencyKey
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Key = idempotencyKey,
            Fingerprint = fingerprint,
            PaymentId = paymentId,
            CreatedAt = clock.GetUtcNow()
        });

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Fallback: a concurrent request won the unique index; return its reservation.
            PaymentIdempotencyKey? winner = await FindAsync(customerId, idempotencyKey, cancellationToken);

            if (winner is null)
                throw;

            return Result<IdempotencyReservation>.Success(new IdempotencyReservation(
                winner.PaymentId, IsNew: false, FingerprintMatches: winner.Fingerprint == fingerprint));
        }

        return Result<IdempotencyReservation>.Success(
            new IdempotencyReservation(paymentId, IsNew: true, FingerprintMatches: true));
    }

    /// <inheritdoc />
    public async Task<Result> ReleaseAsync(
        Guid customerId,
        string idempotencyKey,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        PaymentIdempotencyKey? row = await context.Set<PaymentIdempotencyKey>()
            .FirstOrDefaultAsync(
                item => item.CustomerId == customerId
                    && item.Key == idempotencyKey
                    && item.PaymentId == paymentId,
                cancellationToken);

        if (row is null)
            return Result.Ok();

        context.Set<PaymentIdempotencyKey>().Remove(row);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    /// <summary>Finds an existing reservation.</summary>
    private Task<PaymentIdempotencyKey?> FindAsync(
        Guid customerId,
        string key,
        CancellationToken cancellationToken)
        => context.Set<PaymentIdempotencyKey>().AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.CustomerId == customerId && row.Key == key,
                cancellationToken);
}
