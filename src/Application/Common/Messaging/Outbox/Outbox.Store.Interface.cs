namespace Application.Common.Messaging.Outbox;

/// <summary>Stages and claims durable outbox messages.</summary>
public interface IOutboxStore
{
    Task<Result> EnqueueAsync(
        IReadOnlyCollection<OutboxMessage> messages,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<OutboxMessage>>> ClaimAsync(
        int batchSize,
        DateTimeOffset nowUtc,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task<Result> MarkProcessedAsync(
        Guid id,
        DateTimeOffset processedOnUtc,
        CancellationToken cancellationToken = default);

    Task<Result> MarkFailedAsync(
        Guid id,
        string error,
        DateTimeOffset nextAttemptUtc,
        CancellationToken cancellationToken = default);

    Task<Result> DeadLetterAsync(
        Guid id,
        string error,
        CancellationToken cancellationToken = default);

    Task<Result<int>> CountPendingAsync(CancellationToken cancellationToken = default);

    Task<Result<DateTimeOffset?>> OldestPendingOnUtcAsync(
        CancellationToken cancellationToken = default);

    Task<Result<int>> DeleteProcessedBeforeAsync(
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken = default);
}
