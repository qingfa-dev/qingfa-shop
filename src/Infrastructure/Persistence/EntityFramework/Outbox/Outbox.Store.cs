using Application.Common.Messaging.Outbox;
using Application.Common.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence.EntityFramework.Outbox;

/// <summary>EF Core implementation of <see cref="IOutboxStore"/>.</summary>
/// <remarks>
/// <see cref="EnqueueAsync"/> only stages rows so the caller's unit of work commits them with the
/// state change. Claims lock rows with <c>FOR UPDATE SKIP LOCKED</c> and every claim mints a fresh
/// lease, so marks from a stale worker cannot overwrite a row another worker now owns.
/// </remarks>
public sealed class EfCoreOutboxStore : IOutboxStore
{
    // Claim: the exact spec predicate. Column names mirror OutboxMessageConfiguration.
    private const string ClaimSql =
        "SELECT * FROM " + PersistenceConstant.Schema.Messaging + ".outbox_messages " +
        "WHERE status = {0} AND available_on_utc <= {1} " +
        "AND (locked_until_utc IS NULL OR locked_until_utc < {1}) " +
        "ORDER BY occurred_on_utc LIMIT {2} FOR UPDATE SKIP LOCKED";

    private readonly DbContext _context;

    /// <summary>Creates an outbox store over the supplied context.</summary>
    /// <param name="context">The context that stages and claims outbox rows.</param>
    public EfCoreOutboxStore(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
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
            // Stage only: the transaction that owns the state change saves these rows.
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
            await using IDbContextTransaction transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            List<OutboxMessage> claimed = await Messages
                .FromSqlRaw(ClaimSql, (int)OutboxMessageStatus.Pending, nowUtc, batchSize)
                .ToListAsync(cancellationToken);

            DateTimeOffset lockedUntilUtc = nowUtc.Add(leaseDuration);
            foreach (OutboxMessage message in claimed)
            {
                // Lease: every claim mints a new fencing token for this worker.
                message.LeaseId = Guid.NewGuid();
                message.LockedUntilUtc = lockedUntilUtc;
            }

            if (claimed.Count > 0)
                await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return Result<IReadOnlyList<OutboxMessage>>.Success(claimed);
        }
        catch (Exception exception)
        {
            // Dispose: leaving the scope rolls back the claim transaction.
            return Result<IReadOnlyList<OutboxMessage>>.Fail(
                OutboxOutcome.Failure.ClaimFailed(exception));
        }
    }

    /// <inheritdoc />
    public Task<Result> MarkProcessedAsync(
        Guid id,
        Guid leaseId,
        DateTimeOffset processedOnUtc,
        CancellationToken cancellationToken = default)
        => MarkAsync(
            id,
            leaseId,
            message =>
            {
                message.Status = OutboxMessageStatus.Processed;
                message.ProcessedOnUtc = processedOnUtc;
                message.LeaseId = null;
                message.LockedUntilUtc = null;
                message.LastError = null;
            },
            exception => OutboxOutcome.Failure.MarkFailed(id, exception.Message),
            cancellationToken);

    /// <inheritdoc />
    public Task<Result> MarkFailedAsync(
        Guid id,
        Guid leaseId,
        string error,
        DateTimeOffset nextAttemptUtc,
        CancellationToken cancellationToken = default)
        => MarkAsync(
            id,
            leaseId,
            message =>
            {
                message.Status = OutboxMessageStatus.Pending;
                message.Attempts++;
                message.LastError = error;
                message.AvailableOnUtc = nextAttemptUtc;
                message.LeaseId = null;
                message.LockedUntilUtc = null;
            },
            exception => OutboxOutcome.Failure.MarkFailed(id, exception.Message),
            cancellationToken);

    /// <inheritdoc />
    public Task<Result> DeadLetterAsync(
        Guid id,
        Guid leaseId,
        string error,
        CancellationToken cancellationToken = default)
        => MarkAsync(
            id,
            leaseId,
            message =>
            {
                message.Status = OutboxMessageStatus.DeadLettered;
                message.Attempts++;
                message.LastError = error;
                message.LeaseId = null;
                message.LockedUntilUtc = null;
            },
            exception => OutboxOutcome.Failure.DeadLetterFailed(id, exception.Message),
            cancellationToken);

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
        catch (Exception exception)
        {
            return Result<int>.Fail(TranslateQueryFailure(exception));
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
        catch (Exception exception)
        {
            return Result<DateTimeOffset?>.Fail(TranslateQueryFailure(exception));
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
        catch (Exception exception)
        {
            return Result<int>.Fail(TranslateQueryFailure(exception));
        }
    }

    /// <summary>
    /// Applies a mark only while the caller still owns a live lease; every other case is classified
    /// as lease lost or not found so the caller never sees a silent success.
    /// </summary>
    private async Task<Result> MarkAsync(
        Guid id,
        Guid leaseId,
        Action<OutboxMessage> mutate,
        Func<Exception, Error> translate,
        CancellationToken cancellationToken)
    {
        try
        {
            DateTimeOffset nowUtc = DateTimeOffset.UtcNow;

            OutboxMessage? message = await Messages.FirstOrDefaultAsync(
                candidate => candidate.Id == id
                    && candidate.LeaseId == leaseId
                    && candidate.LockedUntilUtc > nowUtc,
                cancellationToken);

            if (message is null)
                return Result.Fail(await ClassifyMissAsync(id, cancellationToken));

            mutate(message);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Fence: another worker re-claimed the row between the load and the save.
            return Result.Fail(OutboxOutcome.Failure.LeaseLost(id));
        }
        catch (Exception exception)
        {
            return Result.Fail(translate(exception));
        }
    }

    /// <summary>Distinguishes an unknown message from a message owned by another lease.</summary>
    private async Task<Error> ClassifyMissAsync(Guid id, CancellationToken cancellationToken)
    {
        bool exists = await Messages.AnyAsync(
            message => message.Id == id,
            cancellationToken);

        return exists
            ? OutboxOutcome.Failure.LeaseLost(id)
            : OutboxOutcome.Failure.NotFound(id);
    }

    /// <summary>
    /// Translate: read-only outbox queries have no dedicated stable code, so the exception identity
    /// is preserved instead of inventing a new outcome.
    /// </summary>
    private static Error TranslateQueryFailure(Exception exception) => Error.FromException(exception);
}