using System.Data;

using Application.Common.Persistence.UnitOfWorks;

using Infrastructure.Persistence.EntityFramework.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence.EntityFramework.UnitOfWorks;

/// <summary>
/// EF Core implementation of a unit of work transaction.
/// Wraps IDbContextTransaction and ensures rollback on dispose if not committed.
/// </summary>
public sealed class EfCoreTransaction : ITransaction
{
    private readonly EfCoreDbContext _context;
    private readonly IDbContextTransaction _transaction;

    private TransactionState _state = TransactionState.Active;
    private bool _disposed;

    /// <summary>
    /// Creates a new EfCoreTransaction.
    /// </summary>
    /// <param name="context">The EF Core DbContext.</param>
    /// <param name="transaction">The underlying EF Core transaction.</param>
    /// <param name="readOnly">Whether the transaction is read-only.</param>
    internal EfCoreTransaction(
        EfCoreDbContext context,
        IDbContextTransaction transaction,
        bool readOnly)
    {
        _context = context;
        _transaction = transaction;
        IsReadOnly = readOnly;
    }

    /// <inheritdoc />
    public TransactionState State => _state;

    /// <inheritdoc />
    public IsolationLevel IsolationLevel => _transaction.GetDbTransaction().IsolationLevel;

    /// <inheritdoc />
    public bool IsReadOnly { get; }

    /// <inheritdoc />
    /// <remarks>
    /// This method saves pending changes via <c>SaveChangesAsync</c> before committing the underlying
    /// database transaction. This is the expected commit flow: changes staged during the transaction
    /// are persisted as part of the commit. Callers should not call <c>SaveChangesAsync</c> separately
    /// before committing, as this method handles it.
    /// </remarks>
    public async Task<Result> CommitAsync(CancellationToken cancellationToken = default)
    {
        Result? activeCheck = EnsureActive();
        if (activeCheck is not null && activeCheck.IsFailure)
            return activeCheck;

        if (_state == TransactionState.RollbackOnly)
        {
            return Result.Fail(TransactionOutcome.Failure.TransactionRollbackOnly);
        }

        try
        {
            // Save any pending changes before committing the transaction
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
            _state = TransactionState.Committed;
            return Result.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Fail(TransactionOutcome.Failure.ConcurrencyConflict);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Result.Fail(TransactionOutcome.Failure.DuplicateKey);
        }
        catch (DbUpdateException ex)
        {
            return Result.Fail(TranslateDbUpdateException(ex));
        }
        catch (Npgsql.PostgresException pgEx)
        {
            return Result.Fail(TranslatePostgresException(pgEx));
        }
        catch (TimeoutException)
        {
            return Result.Fail(TransactionOutcome.Failure.Timeout);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Fail(TransactionOutcome.Failure.Timeout);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail(TransactionOutcome.Failure.Cancelled);
        }
        catch (Exception ex)
        {
            return Result.Fail(TranslateException(ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result> RollbackAsync(CancellationToken cancellationToken = default)
    {
        Result? activeCheck = EnsureActive();
        if (activeCheck is not null && activeCheck.IsFailure)
            return activeCheck;

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
            _state = TransactionState.RolledBack;
            return Result.Ok();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Fail(TransactionOutcome.Failure.Timeout);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail(TransactionOutcome.Failure.Cancelled);
        }
        catch (Exception ex)
        {
            return Result.Fail(TranslateException(ex));
        }
    }

    /// <inheritdoc />
    public Result MarkRollbackOnly()
    {
        Result? activeCheck = EnsureActive();
        if (activeCheck is not null && activeCheck.IsFailure)
            return activeCheck;
        _state = TransactionState.RollbackOnly;
        return Result.Ok();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Rollback if not already committed or rolled back
        if (_state == TransactionState.Active || _state == TransactionState.RollbackOnly)
        {
            try
            {
                await _transaction.RollbackAsync();
            }
            catch
            {
                // Ignore rollback failures during disposal
            }
        }

        _state = TransactionState.Disposed;
        await _transaction.DisposeAsync();
    }

    /// <summary>
    /// Checks whether the transaction is still active. Returns a failure Result when the
    /// transaction has already been committed, rolled back, or disposed.
    /// </summary>
    private Result? EnsureActive()
    {
        if (_state != TransactionState.Active && _state != TransactionState.RollbackOnly)
        {
            return Result.Fail(TransactionOutcome.Failure.TransactionAlreadyCompleted);
        }
        return null;
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
        if (ex.InnerException is Npgsql.PostgresException pgEx)
            return TranslatePostgresException(pgEx);

        if (ex is DbUpdateConcurrencyException)
            return TransactionOutcome.Failure.ConcurrencyConflict;

        if (ex is DbUpdateException dbEx)
            return TranslateDbUpdateException(dbEx);

        if (ex is TimeoutException)
            return TransactionOutcome.Failure.Timeout;

        if (ex is OperationCanceledException ocEx)
            return ocEx.CancellationToken.IsCancellationRequested
                ? TransactionOutcome.Failure.Cancelled
                : TransactionOutcome.Failure.Timeout;

        if (ex is InvalidOperationException ioe &&
            ioe.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            return TransactionOutcome.Failure.Timeout;

        if (ex is InvalidOperationException ioe2 &&
            (ioe2.Message.Contains("translate", StringComparison.OrdinalIgnoreCase) ||
             ioe2.Message.Contains("LINQ", StringComparison.OrdinalIgnoreCase)))
            return TransactionOutcome.Failure.ExpressionTranslationFailed(ioe2.Message);

        if (ex is InvalidOperationException ioe3)
            return TransactionOutcome.Failure.InvalidOperationFor(ioe3.Message);

        return TransactionOutcome.Failure.Unknown;
    }

    /// <summary>Translates PostgreSQL SQLSTATE codes to persistence errors.</summary>
    static Error TranslatePostgresException(Npgsql.PostgresException ex) => ex.SqlState switch
    {
        "23505" => TransactionOutcome.Failure.DuplicateKey,
        "23503" => TransactionOutcome.Failure.ForeignKeyViolation,
        "23514" => TransactionOutcome.Failure.CheckConstraintViolation,
        "23502" => TransactionOutcome.Failure.NotNullViolation,
        "40P01" => TransactionOutcome.Failure.DeadlockDetected,
        "40001" => TransactionOutcome.Failure.SerializationFailure,
        "08001" => TransactionOutcome.Failure.ConnectionRefused,
        "08006" => TransactionOutcome.Failure.ConnectionLost,
        "28000" => TransactionOutcome.Failure.LoginFailed,
        "08004" => TransactionOutcome.Failure.ConnectionFailed,
        _ => TransactionOutcome.Failure.Unknown,
    };

    /// <summary>Translates DbUpdateException without a known inner exception.</summary>
    static Error TranslateDbUpdateException(DbUpdateException ex)
    {
        string message = ex.Message.ToLowerInvariant();
        if (message.Contains("unique") || message.Contains("duplicate"))
            return TransactionOutcome.Failure.DuplicateKey;
        if (message.Contains("foreign key") || message.Contains("reference constraint"))
            return TransactionOutcome.Failure.ForeignKeyViolation;
        if (message.Contains("check constraint"))
            return TransactionOutcome.Failure.CheckConstraintViolation;
        if (message.Contains("not null") || message.Contains("null value"))
            return TransactionOutcome.Failure.NotNullViolation;
        return TransactionOutcome.Failure.SaveChangesFailed(ex.Message);
    }

}
