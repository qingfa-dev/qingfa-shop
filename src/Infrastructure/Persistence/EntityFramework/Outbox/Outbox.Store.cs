using Application.Common.Messaging.Outbox;
using Application.Common.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Outbox;

/// <summary>EF Core implementation of <see cref="IOutboxStore"/>.</summary>
/// <remarks>
/// <see cref="EnqueueAsync"/> only stages rows so the caller's unit of work commits them with the
/// state change. Claims lock rows with <c>FOR UPDATE SKIP LOCKED</c> and every claim mints a fresh
/// lease. Marks are set-based conditional updates, so a stale worker cannot overwrite a row another
/// worker now owns and cannot leave a poisoned entity in the change tracker.
/// </remarks>
public sealed class EfCoreOutboxStore : IOutboxStore
{
    // Lock: the spec predicate. Column names mirror OutboxMessageConfiguration.
    private const string ClaimSql =
        "WITH claimed AS (" +
        "SELECT id FROM " + PersistenceConstant.Schema.Messaging + ".outbox_messages " +
        "WHERE status = {0} AND available_on_utc <= {1} " +
        "AND (locked_until_utc IS NULL OR locked_until_utc < {1}) " +
        "ORDER BY occurred_on_utc LIMIT {2} FOR UPDATE SKIP LOCKED) " +
        "UPDATE " + PersistenceConstant.Schema.Messaging + ".outbox_messages " +
        "SET lease_id = gen_random_uuid(), locked_until_utc = {3} " +
        "WHERE id IN (SELECT id FROM claimed) RETURNING *";

    private readonly DbContext _context;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates an outbox store over the supplied context and clock.</summary>
    /// <param name="context">The context that stages and claims outbox rows.</param>
    /// <param name="timeProvider">The clock used to evaluate lease expiry.</param>
    public EfCoreOutboxStore(DbContext context, TimeProvider timeProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    private DbSet<OutboxMessage> Messages => _context.Set<OutboxMessage>();

    /// <inheritdoc />
    public Task<Result> EnqueueAsync(
        IReadOnlyCollection<OutboxMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        try
        {
            // Enqueue: stage only; the transaction that owns the state change saves these rows.
            if (messages.Count > 0)
                Messages.AddRange(messages);

            return Task.FromResult(Result.Ok());
        }
        catch (Exception exception)
        {
            return Task.FromResult(
                Result.Fail(OutboxOutcome.Failure.EnqueueFailed(exception)));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<OutboxMessage>>> ClaimAsync(
        int batchSize,
        DateTimeOffset nowUtc,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(leaseDuration, TimeSpan.Zero);

        try
        {
            // Lock: one statement claims, re-leases, and returns the rows; nothing stays tracked.
            List<OutboxMessage> claimed = await Messages
                .FromSqlRaw(
                    ClaimSql,
                    (int)OutboxMessageStatus.Pending,
                    nowUtc,
                    batchSize,
                    nowUtc.Add(leaseDuration))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return Result<IReadOnlyList<OutboxMessage>>.Success(claimed);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<IReadOnlyList<OutboxMessage>>.Fail(
                OutboxOutcome.Failure.ClaimFailed(exception));
        }
    }

    /// <inheritdoc />
    public async Task<Result> MarkProcessedAsync(
        Guid id,
        Guid leaseId,
        DateTimeOffset processedOnUtc,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();

        try
        {
            // Update: the lease predicate makes the mark atomic; no tracked entity is mutated.
            int affected = await Messages
                .Where(message => message.Id == id
                    && message.LeaseId == leaseId
                    && message.LockedUntilUtc > nowUtc)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.Status, OutboxMessageStatus.Processed)
                        .SetProperty(message => message.ProcessedOnUtc, processedOnUtc)
                        .SetProperty(message => message.LeaseId, (Guid?)null)
                        .SetProperty(message => message.LockedUntilUtc, (DateTimeOffset?)null)
                        .SetProperty(message => message.LastError, (string?)null),
                    cancellationToken);

            return await FinishMarkAsync(id, affected, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result.Fail(OutboxOutcome.Failure.MarkFailed(id, exception.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> MarkFailedAsync(
        Guid id,
        Guid leaseId,
        string error,
        DateTimeOffset nextAttemptUtc,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();

        try
        {
            // Update: lease-fenced retry state; the dispatcher owns the attempt count.
            int affected = await Messages
                .Where(message => message.Id == id
                    && message.LeaseId == leaseId
                    && message.LockedUntilUtc > nowUtc)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.Status, OutboxMessageStatus.Pending)
                        .SetProperty(message => message.Attempts, message => message.Attempts + 1)
                        .SetProperty(message => message.LastError, error)
                        .SetProperty(message => message.AvailableOnUtc, nextAttemptUtc)
                        .SetProperty(message => message.LeaseId, (Guid?)null)
                        .SetProperty(message => message.LockedUntilUtc, (DateTimeOffset?)null),
                    cancellationToken);

            return await FinishMarkAsync(id, affected, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result.Fail(OutboxOutcome.Failure.MarkFailed(id, exception.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeadLetterAsync(
        Guid id,
        Guid leaseId,
        string error,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();

        try
        {
            // Update: the terminal transition does not touch the dispatcher-owned attempt count.
            int affected = await Messages
                .Where(message => message.Id == id
                    && message.LeaseId == leaseId
                    && message.LockedUntilUtc > nowUtc)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.Status, OutboxMessageStatus.DeadLettered)
                        .SetProperty(message => message.LastError, error)
                        .SetProperty(message => message.LeaseId, (Guid?)null)
                        .SetProperty(message => message.LockedUntilUtc, (DateTimeOffset?)null),
                    cancellationToken);

            return await FinishMarkAsync(id, affected, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result.Fail(OutboxOutcome.Failure.DeadLetterFailed(id, exception.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<int>> CountPendingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            int count = await Messages.CountAsync(
                message => message.Status == OutboxMessageStatus.Pending,
                cancellationToken);

            return Result<int>.Success(count);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<int>.Fail(OutboxOutcome.Failure.QueryFailed(exception));
        }
    }

    /// <inheritdoc />
    public async Task<Result<DateTimeOffset?>> OldestPendingOnUtcAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            DateTimeOffset? oldest = await Messages
                .Where(message => message.Status == OutboxMessageStatus.Pending)
                .OrderBy(message => message.OccurredOnUtc)
                .Select(message => (DateTimeOffset?)message.OccurredOnUtc)
                .FirstOrDefaultAsync(cancellationToken);

            return Result<DateTimeOffset?>.Success(oldest);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<DateTimeOffset?>.Fail(OutboxOutcome.Failure.QueryFailed(exception));
        }
    }

    /// <inheritdoc />
    public async Task<Result<int>> DeleteProcessedBeforeAsync(
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            int deleted = await Messages
                .Where(message => message.Status == OutboxMessageStatus.Processed
                    && message.ProcessedOnUtc < cutoffUtc)
                .ExecuteDeleteAsync(cancellationToken);

            return Result<int>.Success(deleted);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<int>.Fail(OutboxOutcome.Failure.RetentionFailed(exception));
        }
    }

    /// <summary>
    /// Classifies a zero-row mark: the row exists but this worker no longer owns a live lease, or the
    /// id never existed. The check is untracked, so it cannot poison a later mark in the same batch.
    /// </summary>
    private async Task<Result> FinishMarkAsync(
        Guid id,
        int affected,
        CancellationToken cancellationToken)
    {
        if (affected > 0)
            return Result.Ok();

        bool exists = await Messages.AnyAsync(
            message => message.Id == id,
            cancellationToken);

        return Result.Fail(exists
            ? OutboxOutcome.Failure.LeaseLost(id)
            : OutboxOutcome.Failure.NotFound(id));
    }
}