using Application.Modules.Payments.Persistence;

using Core.Modules.Payments;
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
            ReceivedAt = clock.GetUtcNow().UtcDateTime
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
        row.ProcessedAt = clock.GetUtcNow().UtcDateTime;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result<int>> DeleteProcessedBeforeAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken)
    {
        DateTime cutoffUtc = cutoff.UtcDateTime;

        try
        {
            // Enforce: only settled rows are pruned; an unprocessed delivery may still be retried.
            int deleted = await context.Set<PaymentWebhookEvent>()
                .Where(row => row.ProcessedAt != null && row.ReceivedAt < cutoffUtc)
                .ExecuteDeleteAsync(cancellationToken);

            return Result<int>.Success(deleted);
        }
        catch (DbUpdateException exception)
        {
            return Result<int>.Fail(PaymentsOutcome.Failure.RetentionFailed(exception.Message));
        }
    }
}
