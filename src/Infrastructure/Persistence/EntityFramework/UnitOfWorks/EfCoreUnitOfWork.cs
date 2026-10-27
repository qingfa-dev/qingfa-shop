using System.Data;

using Application.Common.Persistence.UnitOfWorks;

using Infrastructure.Persistence.EntityFramework.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Npgsql;

namespace Infrastructure.Persistence.EntityFramework.UnitOfWorks;

/// <summary>
/// EF Core implementation of the unit of work pattern.
/// Manages DbContext lifecycle, change tracking, and transactions.
/// </summary>
public sealed class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly EfCoreDbContext _context;

    public EfCoreUnitOfWork(EfCoreDbContext context)
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
        catch (Exception ex)
        {
            // Single translation path: concurrency, constraint violations, deadlocks,
            // serialization failures, timeouts and cancellation all map to typed errors.
            return Result.Fail(TranslateException(ex, cancellationToken));
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
            return Result<ITransaction>.Fail(UnitOfWorkOutcome.Failure.TransactionNested);

        try
        {
            IDbContextTransaction transaction = isolationLevel.HasValue
                ? await _context.Database.BeginTransactionAsync(isolationLevel.Value, cancellationToken)
                : await _context.Database.BeginTransactionAsync(cancellationToken);

            return Result<ITransaction>.Success(new EfCoreTransaction(_context, transaction, readOnly));
        }
        catch (Exception ex)
        {
            return Result<ITransaction>.Fail(TranslateException(ex, cancellationToken));
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
                return Result<T>.Fail(UnitOfWorkOutcome.Failure.TransactionRollbackOnly);
            }

            Result commitResult = await transaction.CommitAsync(cancellationToken);
            if (commitResult.IsFailure)
            {
                await TryRollbackAndClearAsync(transaction);
                return Result<T>.Fail(commitResult.Errors);
            }

            return Result<T>.Success(result);
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
    /// Translates raw exceptions into typed persistence errors.
    /// Checks concurrency first (it derives from DbUpdateException), then walks the exception chain
    /// for PostgresException, DbUpdateException, cancellation, and timeout scenarios.
    /// </summary>
    private static Error TranslateException(Exception ex, CancellationToken cancellationToken = default)
    {
        // Concurrency first: it derives from DbUpdateException.
        if (ex is DbUpdateConcurrencyException)
            return UnitOfWorkOutcome.Failure.ConcurrencyConflict;

        // Walk the whole chain; the PostgresException is not always the direct inner exception.
        if (FindInChain<PostgresException>(ex) is { } pgEx)
            return TranslatePostgresException(pgEx);

        if (ex is DbUpdateException dbEx)
            return TranslateDbUpdateException(dbEx);

        // Cancellation: caller's token => Cancelled, anything else (DB-side cancel) => Timeout.
        if (ex is OperationCanceledException oce)
        {
            return cancellationToken.IsCancellationRequested || oce.CancellationToken.IsCancellationRequested
                ? UnitOfWorkOutcome.Failure.Cancelled
                : UnitOfWorkOutcome.Failure.Timeout;
        }

        // Npgsql wraps command timeouts as NpgsqlException -> TimeoutException.
        if (FindInChain<TimeoutException>(ex) is not null)
            return UnitOfWorkOutcome.Failure.Timeout;

        if (ex is InvalidOperationException ioe)
        {
            string msg = ioe.Message;

            if (msg.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                return UnitOfWorkOutcome.Failure.Timeout;

            if (msg.Contains("translate", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("LINQ", StringComparison.OrdinalIgnoreCase))
                return UnitOfWorkOutcome.Failure.ExpressionTranslationFailed(msg);

            return UnitOfWorkOutcome.Failure.InvalidOperationFor(msg);
        }

        return UnitOfWorkOutcome.Failure.SaveChangesFailed(ex.Message);
    }

    /// <summary>
    /// Walks the InnerException chain to find the first exception of type T.
    /// Used because PostgresException is often nested deep inside Npgsql wrappers.
    /// </summary>
    private static TException? FindInChain<TException>(Exception? ex) where TException : Exception
    {
        while (ex is not null)
        {
            if (ex is TException match)
                return match;

            ex = ex.InnerException;
        }

        return null;
    }

    /// <summary>
    /// Translates PostgreSQL SQLSTATE error codes to typed persistence errors.
    /// Covers integrity violations, concurrency, timeouts, connection failures, and authentication errors.
    /// </summary>
    private static Error TranslatePostgresException(PostgresException ex) => ex.SqlState switch
    {
        // Integrity constraint violations (use the constraint/column when Npgsql provides it)
        PostgresErrorCodes.UniqueViolation => ex.ConstraintName is { } c
            ? UnitOfWorkOutcome.Failure.DuplicateKeyFor(c)
            : UnitOfWorkOutcome.Failure.DuplicateKey,
        PostgresErrorCodes.ForeignKeyViolation => ex.ConstraintName is { } c
            ? UnitOfWorkOutcome.Failure.ForeignKeyViolationFor(c)
            : UnitOfWorkOutcome.Failure.ForeignKeyViolation,
        PostgresErrorCodes.CheckViolation => ex.ConstraintName is { } c
            ? UnitOfWorkOutcome.Failure.CheckConstraintViolationFor(c)
            : UnitOfWorkOutcome.Failure.CheckConstraintViolation,
        PostgresErrorCodes.NotNullViolation => ex.ColumnName is { } col
            ? UnitOfWorkOutcome.Failure.NotNullViolationFor(col)
            : UnitOfWorkOutcome.Failure.NotNullViolation,

        // Concurrency
        PostgresErrorCodes.DeadlockDetected => UnitOfWorkOutcome.Failure.DeadlockDetected,
        PostgresErrorCodes.SerializationFailure => UnitOfWorkOutcome.Failure.SerializationFailure,

        // Timeouts / cancelled statements (statement_timeout, lock_timeout)
        PostgresErrorCodes.QueryCanceled => UnitOfWorkOutcome.Failure.Timeout,
        PostgresErrorCodes.LockNotAvailable => UnitOfWorkOutcome.Failure.Timeout,

        // Connection exceptions
        PostgresErrorCodes.SqlClientUnableToEstablishSqlConnection => UnitOfWorkOutcome.Failure.ConnectionRefused,
        PostgresErrorCodes.ConnectionFailure => UnitOfWorkOutcome.Failure.ConnectionLost,
        PostgresErrorCodes.ConnectionException => UnitOfWorkOutcome.Failure.ConnectionFailed,
        PostgresErrorCodes.ConnectionDoesNotExist => UnitOfWorkOutcome.Failure.ConnectionLost,
        PostgresErrorCodes.SqlServerRejectedEstablishmentOfSqlConnection => UnitOfWorkOutcome.Failure.ConnectionFailed,
        PostgresErrorCodes.AdminShutdown => UnitOfWorkOutcome.Failure.ConnectionFailed,
        PostgresErrorCodes.CrashShutdown => UnitOfWorkOutcome.Failure.ConnectionFailed,
        PostgresErrorCodes.CannotConnectNow => UnitOfWorkOutcome.Failure.ConnectionFailed,
        PostgresErrorCodes.TooManyConnections => UnitOfWorkOutcome.Failure.ConnectionFailed,

        // Authentication
        PostgresErrorCodes.InvalidAuthorizationSpecification => UnitOfWorkOutcome.Failure.LoginFailed,
        PostgresErrorCodes.InvalidPassword => UnitOfWorkOutcome.Failure.LoginFailed,

        _ => UnitOfWorkOutcome.Failure.Unknown,
    };

    /// <summary>
    /// Fallback for DbUpdateException when no PostgresException is found in the chain.
    /// Uses message keyword matching to infer the constraint type violated.
    /// </summary>
    private static Error TranslateDbUpdateException(DbUpdateException ex)
    {
        string message = ex.Message.ToLowerInvariant();
        if (message.Contains("unique") || message.Contains("duplicate"))
            return UnitOfWorkOutcome.Failure.DuplicateKey;
        if (message.Contains("foreign key") || message.Contains("reference constraint"))
            return UnitOfWorkOutcome.Failure.ForeignKeyViolation;
        if (message.Contains("check constraint"))
            return UnitOfWorkOutcome.Failure.CheckConstraintViolation;
        if (message.Contains("not null") || message.Contains("null value"))
            return UnitOfWorkOutcome.Failure.NotNullViolation;
        return UnitOfWorkOutcome.Failure.SaveChangesFailed(ex.Message);
    }
}