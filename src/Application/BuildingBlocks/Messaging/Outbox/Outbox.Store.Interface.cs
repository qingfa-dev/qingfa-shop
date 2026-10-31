namespace Application.BuildingBlocks.Messaging.Outbox;

/// <summary>Stages and claims durable outbox messages.</summary>
/// <remarks>
/// <para>
/// <see cref="ClaimAsync"/> assigns each returned message a fresh <see cref="OutboxMessage.LeaseId"/>
/// and extends <see cref="OutboxMessage.LockedUntilUtc"/> by the requested lease duration; only
/// unprocessed messages whose lock has expired or was never taken are claimable.
/// </para>
/// <para>
/// The mark operations (<see cref="MarkProcessedAsync"/>, <see cref="MarkFailedAsync"/>, and
/// <see cref="DeadLetterAsync"/>) are conditional on the supplied <c>leaseId</c> still matching the
/// stored lease and on <see cref="OutboxMessage.LockedUntilUtc"/> not having passed. A stale or
/// expired mark changes nothing and returns <see cref="OutboxOutcome.Codes.LeaseLost"/> via
/// <see cref="OutboxOutcome.Failure.LeaseLost"/>, so a worker that lost its lease cannot overwrite
/// the state of a message another worker now owns.
/// </para>
/// </remarks>
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

    /// <summary>Marks a claimed message processed when the caller still owns its lease.</summary>
    /// <param name="id">The message id.</param>
    /// <param name="leaseId">The lease returned by <see cref="ClaimAsync"/>.</param>
    /// <param name="processedOnUtc">The processing timestamp.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A successful result; <see cref="OutboxOutcome.Failure.LeaseLost"/> for a stale or expired
    /// lease; <see cref="OutboxOutcome.Failure.NotFound"/> for an unknown message.
    /// </returns>
    Task<Result> MarkProcessedAsync(
        Guid id,
        Guid leaseId,
        DateTimeOffset processedOnUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Reschedules a claimed message after a non-terminal failure.</summary>
    /// <param name="id">The message id.</param>
    /// <param name="leaseId">The lease returned by <see cref="ClaimAsync"/>.</param>
    /// <param name="error">The failure detail persisted on the message.</param>
    /// <param name="nextAttemptUtc">The earliest time the message becomes claimable again.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A successful result; <see cref="OutboxOutcome.Failure.LeaseLost"/> for a stale or expired
    /// lease; <see cref="OutboxOutcome.Failure.NotFound"/> for an unknown message.
    /// </returns>
    Task<Result> MarkFailedAsync(
        Guid id,
        Guid leaseId,
        string error,
        DateTimeOffset nextAttemptUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Moves a claimed message to the dead-letter state.</summary>
    /// <param name="id">The message id.</param>
    /// <param name="leaseId">The lease returned by <see cref="ClaimAsync"/>.</param>
    /// <param name="error">The terminal failure detail persisted on the message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A successful result; <see cref="OutboxOutcome.Failure.LeaseLost"/> for a stale or expired
    /// lease; <see cref="OutboxOutcome.Failure.NotFound"/> for an unknown message.
    /// </returns>
    Task<Result> DeadLetterAsync(
        Guid id,
        Guid leaseId,
        string error,
        CancellationToken cancellationToken = default);

    Task<Result<int>> CountPendingAsync(CancellationToken cancellationToken = default);

    Task<Result<DateTimeOffset?>> OldestPendingOnUtcAsync(
        CancellationToken cancellationToken = default);

    Task<Result<int>> DeleteProcessedBeforeAsync(
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken = default);
}
