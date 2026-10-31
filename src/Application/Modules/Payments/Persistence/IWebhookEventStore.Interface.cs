namespace Application.Modules.Payments.Persistence;

/// <summary>Records provider webhook deliveries for deduplication and audit.</summary>
public interface IWebhookEventStore
{
    /// <summary>Records a delivery; returns false when the event id was already seen.</summary>
    /// <param name="provider">The provider name.</param>
    /// <param name="providerEventId">The provider event id.</param>
    /// <param name="eventType">The provider event type.</param>
    /// <param name="rawBody">The raw request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when this delivery was recorded for the first time.</returns>
    Task<Result<bool>> TryRecordAsync(
        string provider,
        string providerEventId,
        string eventType,
        string rawBody,
        CancellationToken cancellationToken);

    /// <summary>Marks a recorded delivery with its outcome.</summary>
    /// <param name="provider">The provider name.</param>
    /// <param name="providerEventId">The provider event id.</param>
    /// <param name="outcome">The processing outcome.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or a stable failure.</returns>
    Task<Result> MarkProcessedAsync(
        string provider,
        string providerEventId,
        string outcome,
        CancellationToken cancellationToken);
}
