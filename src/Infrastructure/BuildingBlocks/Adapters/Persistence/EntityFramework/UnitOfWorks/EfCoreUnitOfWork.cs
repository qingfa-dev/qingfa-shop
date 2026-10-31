using System.Data;

using Application.BuildingBlocks.Persistence.UnitOfWorks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.UnitOfWorks;

/// <summary>
/// EF Core implementation of the unit of work pattern.
/// Manages DbContext lifecycle, change tracking, and transactions.
/// </summary>
public sealed class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;

    public EfCoreUnitOfWork(DbContext context)
    {
        _context = context;
    }

    /// <summary>True when the change tracker has pending additions, updates, or deletions not yet saved.</summary>
    public bool HasChanges => _context.ChangeTracker.HasChanges();

    /// <inheritdoc />
    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (ResultException ex)
        {
            // Preserve: a save interceptor already produced a structured failure
            // (for example, post-save domain-event dispatch).
            return Result.Fail(ex.Errors.ToList());
        }
        catch (Exception ex)
        {
            // SaveChanges failures collapse to a single write error unless the store
            // reports a condition the caller can act on (concurrency, duplicate, timeout).
            return Result.Fail(TranslateSaveChanges(ex, cancellationToken));
        }
    }

    /// <inheritdoc />
    public async Task<Result<ITransaction>> BeginTransactionAsync(
        IsolationLevel? isolationLevel = null,
        bool readOnly = false,
        CancellationToken cancellationToken = default)
    {
        // Guard: nested transactions are not supported; caller must commit or rollback first.
        if (_context.Database.CurrentTransaction is not null)
            return Result<ITransaction>.Fail(TransactionOutcome.Failure.TransactionNested);

        try
        {
            IDbContextTransaction transaction = isolationLevel.HasValue
                ? await _context.Database.BeginTransactionAsync(isolationLevel.Value, cancellationToken)
                : await _context.Database.BeginTransactionAsync(cancellationToken);

            // Use the requested level, or the provider's resolved level when the caller
            // leaves it to the provider. Non-relational providers expose no DbTransaction.
            IsolationLevel effective = isolationLevel ?? TryReadIsolationLevel(transaction);

            return Result<ITransaction>.Success(
                new EfCoreTransaction(_context, transaction, readOnly, effective));
        }
        catch (Exception ex)
        {
            return Result<ITransaction>.Fail(TranslateException(ex, cancellationToken));
        }
    }

    /// <summary>
    /// Reads the isolation level from a relational provider transaction.
    /// Falls back to <see cref="IsolationLevel.Unspecified"/> when the provider has no
    /// relational transaction to inspect (for example, InMemory or a test double).
    /// </summary>
    private static IsolationLevel TryReadIsolationLevel(IDbContextTransaction transaction)
    {
        try
        {
            return transaction.GetDbTransaction().IsolationLevel;
        }
        catch (InvalidOperationException)
        {
            return IsolationLevel.Unspecified;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Executes the work delegate and saves changes. Delegates to the generic version
    /// so there is a single copy of the commit/rollback/cleanup logic.
    /// </summary>
    public async Task<Result> ExecuteAsync(
        Func<CancellationToken, Task> work,
        CancellationToken cancellationToken = default)
    {
        if (work is null)
            return Result.Fail(UnitOfWorkOutcome.Failure.WorkRequired);

        // Delegate: wrap the void work as a bool-returning delegate so the generic
        // ExecuteAsync<T> holds the single copy of commit/rollback/cleanup logic.
        Result<bool> result = await ExecuteAsync<bool>(async ct =>
        {
            await work(ct);
            return true;
        }, cancellationToken);

        return result.IsFailure ? Result.Fail(result.Errors) : Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> work,
        CancellationToken cancellationToken = default)
    {
        if (work is null)
            return Result<T>.Fail(UnitOfWorkOutcome.Failure.WorkRequired);

        try
        {
            T result = await work(cancellationToken);
            Result saveResult = await SaveChangesAsync(cancellationToken);

            return saveResult.IsFailure
                ? Result<T>.Fail(saveResult.Errors)
                : Result<T>.Success(result);
        }
        catch (ResultException ex)
        {
            // Preserve: the work delegate or a save interceptor returned a structured failure.
            return Result<T>.Fail(ex.Errors.ToList());
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(TranslateException(ex, cancellationToken));
        }
    }

    /// <inheritdoc />
    public async Task<Result> ExecuteInTransactionAsync(
        Func<ITransaction, CancellationToken, Task> work,
        IsolationLevel? isolationLevel = null,
        bool readOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (work is null)
            return Result.Fail(UnitOfWorkOutcome.Failure.WorkRequired);

        // Delegate to the generic implementation so there is exactly one copy of the
        // commit/rollback/cleanup logic (the two copies had drifted apart).
        Result<bool> result = await ExecuteInTransactionAsync<bool>(async (tx, ct) =>
        {
            await work(tx, ct);
            return true;
        }, isolationLevel, readOnly, cancellationToken);

        return result.IsFailure ? Result.Fail(result.Errors) : Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<ITransaction, CancellationToken, Task<T>> work,
        IsolationLevel? isolationLevel = null,
        bool readOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (work is null)
            return Result<T>.Fail(UnitOfWorkOutcome.Failure.WorkRequired);

        Result<ITransaction> txResult = await BeginTransactionAsync(
            isolationLevel, readOnly, cancellationToken);

        if (txResult.IsFailure)
            return Result<T>.Fail(txResult.Errors);

        return await ExecuteInTransactionCoreAsync(txResult.Value, work, cancellationToken);
    }

    /// <summary>
    /// Core transaction execution: runs the work, commits on success, and rolls back on failure.
    /// Always disposes the transaction in the finally block.
    /// </summary>
    private async Task<Result<T>> ExecuteInTransactionCoreAsync<T>(
        ITransaction transaction,
        Func<ITransaction, CancellationToken, Task<T>> work,
        CancellationToken cancellationToken)
    {
        try
        {
            T result = await work(transaction, cancellationToken);

            if (transaction.State == TransactionState.RollbackOnly)
            {
                await TryRollbackAndClearAsync(transaction);
                return Result<T>.Fail(TransactionOutcome.Failure.TransactionRollbackOnly);
            }

            Result commitResult = await transaction.CommitAsync(cancellationToken);
            if (commitResult.IsFailure)
            {
                await TryRollbackAndClearAsync(transaction);
                return Result<T>.Fail(commitResult.Errors);
            }

            return Result<T>.Success(result);
        }
        catch (ResultException ex)
        {
            // Compensate: rollback and discard tracked state, then preserve the structured failure.
            await TryRollbackAndClearAsync(transaction);
            return Result<T>.Fail(ex.Errors.ToList());
        }
        catch (Exception ex)
        {
            // Compensate: rollback (with a non-cancelled token) and discard tracked state.
            await TryRollbackAndClearAsync(transaction);

            // TranslateException maps OCE to Cancelled / Timeout based on the caller's token.
            return Result<T>.Fail(TranslateException(ex, cancellationToken));
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    /// <summary>
    /// Best-effort rollback that never throws, so it cannot mask the original failure.
    /// Always clears the change tracker to prevent stale entities from leaking to the next operation.
    /// </summary>
    private async Task TryRollbackAndClearAsync(ITransaction transaction)
    {
        try
        {
            await RollbackAndClearAsync(transaction);
        }
        catch
        {
            // Preserve the original outcome. Log the rollback failure in production.
        }
    }

    /// <summary>
    /// Rolls back the transaction if active and clears the EF Core change tracker.
    /// Uses a non-cancelled token for rollback so the caller's cancelled token does not abort cleanup.
    /// </summary>
    private async Task RollbackAndClearAsync(ITransaction transaction)
    {
        try
        {
            if (transaction.State is TransactionState.Active or TransactionState.RollbackOnly)
            {
                // Non-cancelled token: the caller's token may already be cancelled,
                // and a cancelled token would make the rollback itself throw.
                await transaction.RollbackAsync(CancellationToken.None);
            }
        }
        finally
        {
            // Rollback does not reset EF Core's tracked entity states.
            _context.ChangeTracker.Clear();
        }
    }

    // ---------------------------------------------------------------------
    // Exception translation
    // ---------------------------------------------------------------------

    /// <summary>
    /// Translates exceptions thrown by caller work or general persistence operations
    /// into the outcome that owns the failure.
    /// </summary>
    private static Error TranslateException(Exception ex, CancellationToken cancellationToken = default) =>
        PersistenceExceptionTranslator.Classify(ex, cancellationToken).ToOutcome();

    /// <summary>
    /// Translates a failed <c>SaveChangesAsync</c>. Only conditions the caller can act on
    /// keep their specific outcome; every other constraint or state failure is a write failure.
    /// </summary>
    private static Error TranslateSaveChanges(Exception ex, CancellationToken cancellationToken)
    {
        PersistenceFailure failure = PersistenceExceptionTranslator.Classify(ex, cancellationToken);

        return failure.Kind switch
        {
            PersistenceFailureKind.ConcurrencyConflict
                or PersistenceFailureKind.DuplicateKey
                or PersistenceFailureKind.Timeout
                or PersistenceFailureKind.Cancelled => failure.ToOutcome(),
            _ => failure.ToSaveChangesError(ex.Message),
        };
    }
}