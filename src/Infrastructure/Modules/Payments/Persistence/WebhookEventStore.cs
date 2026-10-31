using Application.Modules.Payments.Persistence;

using Core.Modules.Payments.Intents;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Payments.Persistence;

/// <summary>Persists webhook deliveries and deduplicates on the provider event id.</summary>
public sealed class WebhookEventStore(DbContext context, TimeProvider clock) : IWebhookEventStore
{
    /// <inheritdoc />
    public async Task<Result<bool>> TryRecordAsync(
        string provider,
        string providerEventId,
        string eventType,
        string rawBody,
        CancellationToken cancellationToken)
    {
        bool exists = await context.Set<PaymentWebhookEvent>()
            .AnyAsync(
                row => row.Provider == provider && row.ProviderEventId == providerEventId,
                cancellationToken);

        if (exists)
            return Result<bool>.Success(false);

        context.Set<PaymentWebhookEvent>().Add(new PaymentWebhookEvent
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            ProviderEventId = providerEventId,
            EventType = eventType,
            RawBody = rawBody,
            Outcome = PaymentConstant.Outcomes.Received,
            ReceivedAt = clock.GetUtcNow()
        });

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Fallback: a concurrent delivery won the unique index; this one is a duplicate.
            return Result<bool>.Success(false);
        }

        return Result<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<Result> MarkProcessedAsync(
        string provider,
        string providerEventId,
        string outcome,
        CancellationToken cancellationToken)
    {
        PaymentWebhookEvent? row = await context.Set<PaymentWebhookEvent>()
            .FirstOrDefaultAsync(
                item => item.Provider == provider && item.ProviderEventId == providerEventId,
                cancellationToken);

        if (row is null)
            return Result.Ok();

        row.Outcome = outcome;
        row.ProcessedAt = clock.GetUtcNow();
        await context.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
