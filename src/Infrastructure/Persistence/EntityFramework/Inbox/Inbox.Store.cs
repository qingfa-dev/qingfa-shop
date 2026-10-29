using Application.Common.Messaging.Inbox;
using Application.Common.Persistence.UnitOfWorks;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Inbox;

/// <summary>EF Core implementation of <see cref="IInboxStore"/>.</summary>
/// <remarks>
/// The duplicate check, the handler, and the processed marker share one transaction, so the
/// marker commits only with the effects it guards. A duplicate delivery is a no-op, and a
/// failed handler leaves no marker behind so the message can be retried.
/// </remarks>
public sealed class EfCoreInboxStore : IInboxStore
{
    private const string ExecutionFailed = "Inbox.Execution.Failed";

    private readonly DbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates an inbox store over the supplied context, unit of work, and clock.</summary>
    /// <param name="context">The context that stages and reads inbox rows.</param>
    /// <param name="unitOfWork">The unit of work that owns the transaction and the save.</param>
    /// <param name="timeProvider">The clock stamped on a processed message.</param>
    public EfCoreInboxStore(DbContext context, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    private DbSet<InboxMessage> Messages => _context.Set<InboxMessage>();

    /// <inheritdoc />
    public async Task<Result> ExecuteOnceAsync(
        Guid messageId,
        string consumer,
        Func<CancellationToken, Task<Result>> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumer);
        ArgumentNullException.ThrowIfNull(handler);

        Result<ITransaction> begun = await _unitOfWork.BeginTransactionAsync(
            cancellationToken: cancellationToken);

        if (begun.IsFailure)
            return Result.Fail(begun.Errors);

        ITransaction transaction = begun.Value;

        try
        {
            // Guard: redelivery is a no-op, so the handler must not run a second time.
            bool processed = await Messages
                .AsNoTracking()
                .AnyAsync(
                    message => message.MessageId == messageId && message.Consumer == consumer,
                    cancellationToken);

            if (processed)
            {
                await RollbackAsync(transaction, clearTracker: false);
                return Result.Ok();
            }

            Result handled = await handler(cancellationToken);

            if (handled.IsFailure)
            {
                // Compensate: a failed handler is never recorded as processed.
                await RollbackAsync(transaction);
                return handled;
            }

            // Create: the marker commits in the same transaction as the handler's own writes.
            Messages.Add(new InboxMessage
            {
                MessageId = messageId,
                Consumer = consumer,
                ProcessedOnUtc = _timeProvider.GetUtcNow(),
            });

            Result saved = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (saved.IsFailure)
            {
                await RollbackAsync(transaction);

                // Fallback: a concurrent consumer committed the same marker first.
                return saved.Errors.Any(error => error.Code == UnitOfWorkOutcome.Codes.DuplicateKey)
                    ? Result.Ok()
                    : Result.Fail(saved.Errors);
            }

            Result committed = await transaction.CommitAsync(cancellationToken);

            if (committed.IsFailure)
            {
                await RollbackAsync(transaction);
                return Result.Fail(committed.Errors);
            }

            return Result.Ok();
        }
        catch (OperationCanceledException)
        {
            // Escalate: cancellation keeps its own meaning and never becomes an internal failure.
            throw;
        }
        catch (Exception exception)
        {
            // Catch: any other failure must leave no marker behind.
            await RollbackAsync(transaction);
            return Result.Fail(Error.FromException(exception, ExecutionFailed));
        }
        finally
        {
            // Dispose: rolls back when the transaction was not committed.
            await transaction.DisposeAsync();
        }
    }

    /// <summary>Best-effort rollback that cannot mask the original outcome.</summary>
    private async Task RollbackAsync(ITransaction transaction, bool clearTracker = true)
    {
        try
        {
            // Compensate: the caller's token may already be cancelled, so cleanup is uncancellable.
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch
        {
            // Preserve: disposal still rolls back a transaction left active.
        }
        finally
        {
            if (clearTracker)
            {
                // Update: rollback does not reset tracked entities, and a failed insert must not linger.
                _context.ChangeTracker.Clear();
            }
        }
    }
}