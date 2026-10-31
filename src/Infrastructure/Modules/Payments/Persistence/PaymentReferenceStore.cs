using Application.Modules.Payments.Persistence;

using Core.Modules.Payments;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Payments.Persistence;

/// <summary>Reads and writes provider-to-payment mappings.</summary>
public sealed class PaymentReferenceStore(DbContext context, TimeProvider clock) : IPaymentReferenceStore
{
    /// <inheritdoc />
    public async Task<Result> RecordAsync(
        PaymentProvider provider,
        string providerReference,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        string name = provider.ToWire();

        bool exists = await context.Set<PaymentReference>()
            .AnyAsync(row => row.Provider == name && row.ProviderReference == providerReference, cancellationToken);

        if (exists)
            return Result.Ok();

        context.Set<PaymentReference>().Add(new PaymentReference
        {
            Id = Guid.NewGuid(),
            Provider = name,
            ProviderReference = providerReference,
            PaymentId = paymentId,
            CreatedAt = clock.GetUtcNow()
        });

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Fallback: another request recorded the same reference first; the mapping is identical.
        }

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> FindAsync(
        PaymentProvider provider,
        string providerReference,
        CancellationToken cancellationToken)
    {
        string name = provider.ToWire();

        Guid? paymentId = await context.Set<PaymentReference>().AsNoTracking()
            .Where(row => row.Provider == name && row.ProviderReference == providerReference)
            .Select(row => (Guid?)row.PaymentId)
            .FirstOrDefaultAsync(cancellationToken);

        return paymentId is { } resolved
            ? Result<Guid>.Success(resolved)
            : Result<Guid>.Fail(PaymentsOutcome.Failure.ReferenceNotFound(providerReference));
    }
}
