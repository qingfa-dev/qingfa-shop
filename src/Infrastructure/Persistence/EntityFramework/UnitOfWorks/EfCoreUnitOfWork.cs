using System.Data;

using Application.Common.Persistence;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;

using Infrastructure.Persistence.EntityFramework.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence.EntityFramework.UnitOfWorks;

/// <summary>
/// EF Core implementation of the unit of work pattern.
/// Manages DbContext lifecycle, change tracking, and transactions.
/// </summary>
public sealed class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly EfCoreDbContext _context;

    /// <summary>
    /// Creates a new EfCoreUnitOfWork.
    /// </summary>
    /// <param name="context">The EF Core DbContext.</param>
    public EfCoreUnitOfWork(EfCoreDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public bool HasChanges => _context.ChangeTracker.HasChanges();

    /// <inheritdoc />
    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Fail(UnitOfWorkOutcome.Failure.ConcurrencyConflict);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Result.Fail(UnitOfWorkOutcome.Failure.DuplicateKey);
        }
        catch (TimeoutException)
        {
            return Result.Fail(UnitOfWorkOutcome.Failure.Timeout);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Fail(UnitOfWorkOutcome.Failure.Timeout);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail(UnitOfWorkOutcome.Failure.Cancelled);
        }
        catch (Exception ex)
        {
            return Result.Fail(UnitOfWorkOutcome.Failure.SaveChangesFailed(ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<ITransaction>> BeginTransactionAsync(
        IsolationLevel? isolationLevel = null,
        bool readOnly = false,
        CancellationToken cancellationToken = default)
    {
        // Check for nested transaction
        if (_context.Database.CurrentTransaction is not null)
        {
            return Result<ITransaction>.Fail(UnitOfWorkOutcome.Failure.TransactionNested);
        }

        try
        {
            IDbContextTransaction transaction;

            if (isolationLevel.HasValue)
            {
                transaction = await _context.Database.BeginTransactionAsync(
                    isolationLevel.Value, cancellationToken);
            }
            else
            {
                transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            }

            var efTransaction = new EfCoreTransaction(_context, transaction, readOnly);
            return Result<ITransaction>.Success(efTransaction);
        }
        catch (Exception ex)
        {
            return Result<ITransaction>.Fail(TranslateException(ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result> ExecuteAsync(
        Func<CancellationToken, Task> work,
        CancellationToken cancellationToken = default)
    {
        // Guard: Null work function would cause NullReferenceException
        if (work is null)
            return Result.Fail(UnitOfWorkOutcome.Failure.WorkRequired);

        try
        {
            await work(cancellationToken);
            return await SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Fail(UnitOfWorkOutcome.Failure.Cancelled);
        }
        catch (Exception ex)
        {
            return Result.Fail(TranslateException(ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> work,
        CancellationToken cancellationToken = default)
    {
        // Guard: Null work function would cause NullReferenceException
        if (work is null)
            return Result<T>.Fail(UnitOfWorkOutcome.Failure.WorkRequired);

        try
        {
            T result = await work(cancellationToken);
            Result saveResult = await SaveChangesAsync(cancellationToken);

            if (saveResult.IsFailure)
                return Result<T>.Fail(saveResult.Errors);

            return Result<T>.Success(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<T>.Fail(UnitOfWorkOutcome.Failure.Cancelled);
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(TranslateException(ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result> ExecuteInTransactionAsync(
        Func<ITransaction, CancellationToken, Task> work,
        IsolationLevel? isolationLevel = null,
        bool readOnly = false,
        CancellationToken cancellationToken = default)
    {
        // Guard: Null work function would cause NullReferenceException
        if (work is null)
            return Result.Fail(UnitOfWorkOutcome.Failure.WorkRequired);

        Result<ITransaction> txResult = await BeginTransactionAsync(
            isolationLevel, readOnly, cancellationToken);

        if (txResult.IsFailure)
            return Result.Fail(txResult.Errors);

        return await ExecuteInTransactionCoreAsync(
            txResult.Value, work, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<ITransaction, CancellationToken, Task<T>> work,
        IsolationLevel? isolationLevel = null,
        bool readOnly = false,
        CancellationToken cancellationToken = default)
    {
        // Guard: Null work function would cause NullReferenceException
        if (work is null)
            return Result<T>.Fail(UnitOfWorkOutcome.Failure.WorkRequired);

        Result<ITransaction> txResult = await BeginTransactionAsync(
            isolationLevel, readOnly, cancellationToken);

        if (txResult.IsFailure)
            return Result<T>.Fail(txResult.Errors);

        return await ExecuteInTransactionCoreAsync<T>(
            txResult.Value, work, cancellationToken);
    }

    private async Task<Result> ExecuteInTransactionCoreAsync(
        ITransaction transaction,
        Func<ITransaction, CancellationToken, Task> work,
        CancellationToken cancellationToken)
    {
        try
        {
            await work(transaction, cancellationToken);

            if (transaction.State == TransactionState.RollbackOnly)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Fail(UnitOfWorkOutcome.Failure.TransactionRollbackOnly);
            }

            Result commitResult = await transaction.CommitAsync(cancellationToken);
            if (commitResult.IsFailure)
                return Result.Fail(commitResult.Errors);

            return Result.Ok();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (transaction.State == TransactionState.Active)
                await transaction.RollbackAsync(cancellationToken);

            return Result.Fail(UnitOfWorkOutcome.Failure.Cancelled);
        }
        catch (Exception ex)
        {
            if (transaction.State == TransactionState.Active)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch
                {
                    // Ignore rollback failures during exception handling
                }
            }

            // Clear change tracker to discard staged but uncommitted entities
            _context.ChangeTracker.Clear();

            return Result.Fail(TranslateException(ex));
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

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
                await transaction.RollbackAsync(cancellationToken);
                return Result<T>.Fail(UnitOfWorkOutcome.Failure.TransactionRollbackOnly);
            }

            Result commitResult = await transaction.CommitAsync(cancellationToken);
            if (commitResult.IsFailure)
                return Result<T>.Fail(commitResult.Errors);

            return Result<T>.Success(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (transaction.State == TransactionState.Active)
                await transaction.RollbackAsync(cancellationToken);

            return Result<T>.Fail(UnitOfWorkOutcome.Failure.Cancelled);
        }
        catch (Exception ex)
        {
            if (transaction.State == TransactionState.Active)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch
                {
                    // Ignore rollback failures during exception handling
                }
            }

            // Clear change tracker to discard staged but uncommitted entities
            _context.ChangeTracker.Clear();

            return Result<T>.Fail(TranslateException(ex));
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        if (ex.InnerException is Npgsql.PostgresException pgEx &&
            pgEx.SqlState == "23505")
            return true;

        string message = ex.Message.ToLowerInvariant();
        return message.Contains("unique") ||
               message.Contains("duplicate") ||
               message.Contains("violation");
    }

    /// <summary>
    /// Translates EF Core and database provider exceptions to granular persistence errors.
    /// </summary>
    private static Error TranslateException(Exception ex)
    {
        // PostgreSQL-specific: inspect the inner PostgresException for precise SQLSTATE codes
        if (ex.InnerException is Npgsql.PostgresException pgEx)
        {
            return TranslatePostgresException(pgEx);
        }

        // EF Core concurrency exception
        if (ex is DbUpdateConcurrencyException)
        {
            return UnitOfWorkOutcome.Failure.ConcurrencyConflict;
        }

        // EF Core update exception (constraint violations, etc.)
        if (ex is DbUpdateException dbEx)
        {
            return TranslateDbUpdateException(dbEx);
        }

        // Timeout
        if (ex is TimeoutException)
        {
            return UnitOfWorkOutcome.Failure.Timeout;
        }

        // Cancellation
        if (ex is OperationCanceledException ocEx)
        {
            return ocEx.CancellationToken.IsCancellationRequested
                ? UnitOfWorkOutcome.Failure.Cancelled
                : UnitOfWorkOutcome.Failure.Timeout;
        }

        // InvalidOperationException with timeout message
        if (ex is InvalidOperationException ioe &&
            ioe.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            return UnitOfWorkOutcome.Failure.Timeout;
        }

        // LINQ expression translation failure
        if (ex is InvalidOperationException ioe2 &&
            (ioe2.Message.Contains("translate", StringComparison.OrdinalIgnoreCase) ||
             ioe2.Message.Contains("LINQ", StringComparison.OrdinalIgnoreCase)))
        {
            return UnitOfWorkOutcome.Failure.ExpressionTranslationFailed(ioe2.Message);
        }

        if (ex is InvalidOperationException ioe3)
        {
            return UnitOfWorkOutcome.Failure.InvalidOperationFor(ioe3.Message);
        }

        // Unknown
        return UnitOfWorkOutcome.Failure.Unknown;
    }

    /// <summary>Translates PostgreSQL SQLSTATE codes to persistence errors.</summary>
    static Error TranslatePostgresException(Npgsql.PostgresException ex) => ex.SqlState switch
    {
        // Integrity constraint violations
        "23505" => UnitOfWorkOutcome.Failure.DuplicateKey,
        "23503" => UnitOfWorkOutcome.Failure.ForeignKeyViolation,
        "23514" => UnitOfWorkOutcome.Failure.CheckConstraintViolation,
        "23502" => UnitOfWorkOutcome.Failure.NotNullViolation,

        // Lock/wait deadlocks
        "40P01" => UnitOfWorkOutcome.Failure.DeadlockDetected,

        // Serialization failures
        "40001" => UnitOfWorkOutcome.Failure.SerializationFailure,

        // Connection exceptions
        "08001" => UnitOfWorkOutcome.Failure.ConnectionRefused,
        "08006" => UnitOfWorkOutcome.Failure.ConnectionLost,

        // Invalid authorization specification
        "28000" => UnitOfWorkOutcome.Failure.LoginFailed,

        // Cannot connect now (server shutting down, etc.)
        "08004" => UnitOfWorkOutcome.Failure.ConnectionFailed,

        // Other PostgreSQL errors
        _ => UnitOfWorkOutcome.Failure.Unknown,
    };

    /// <summary>Translates DbUpdateException without a known inner exception.</summary>
    static Error TranslateDbUpdateException(DbUpdateException ex)
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
