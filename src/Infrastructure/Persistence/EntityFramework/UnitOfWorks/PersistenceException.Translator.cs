using Application.Common.Persistence.UnitOfWorks;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace Infrastructure.Persistence.EntityFramework.UnitOfWorks;

/// <summary>
/// Single place that maps EF Core / Npgsql exceptions to typed persistence errors.
/// Shared by <see cref="EfCoreUnitOfWork"/> and <see cref="EfCoreTransaction"/> so that the
/// same database condition always yields the same error code, whichever path hit it.
/// </summary>
internal static class PersistenceExceptionTranslator
{
    /// <summary>
    /// Single entry point: maps any EF Core / Npgsql exception to a typed persistence error.
    /// Used by both <see cref="EfCoreUnitOfWork"/> and <see cref="EfCoreTransaction"/>
    /// so the same database condition always yields the same error code.
    /// </summary>
    public static Error Translate(Exception ex, CancellationToken cancellationToken = default)
    {
        // Priority: concurrency derives from DbUpdateException, check it before walking the chain.
        if (ex is DbUpdateConcurrencyException)
            return UnitOfWorkOutcome.Failure.ConcurrencyConflict;

        // Walk the whole chain; PostgresException is not always the direct inner exception.
        if (FindInChain<PostgresException>(ex) is { } pgEx)
            return TranslatePostgres(pgEx);

        if (ex is DbUpdateException dbEx)
            return TranslateDbUpdate(dbEx);

        // Cancellation: caller's token => Cancelled; DB-side cancel => Timeout.
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

    /// <summary>Searches the exception chain for a specific exception type.</summary>
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
    /// Translates PostgreSQL SQLSTATE codes to typed persistence errors.
    /// Transaction-state codes (ReadOnlySqlTransaction, InFailedSqlTransaction) map to TransactionOutcome
    /// rather than UnitOfWorkOutcome, because they indicate the transaction itself is in an invalid state,
    /// not a general persistence failure.
    /// </summary>
    private static Error TranslatePostgres(PostgresException ex) => ex.SqlState switch
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

        // Map: transaction-state codes map to TransactionOutcome (not UnitOfWorkOutcome)
        // because they indicate the transaction itself is in an invalid state.
        PostgresErrorCodes.ReadOnlySqlTransaction => TransactionOutcome.Failure.ReadOnlyViolation,
        PostgresErrorCodes.InFailedSqlTransaction => TransactionOutcome.Failure.TransactionRollbackOnly,

        // Timeouts (statement_timeout, lock_timeout, cancelled statements)
        PostgresErrorCodes.QueryCanceled => UnitOfWorkOutcome.Failure.Timeout,
        PostgresErrorCodes.LockNotAvailable => UnitOfWorkOutcome.Failure.Timeout,

        // Connection exceptions
        PostgresErrorCodes.SqlClientUnableToEstablishSqlConnection => UnitOfWorkOutcome.Failure.ConnectionRefused,
        PostgresErrorCodes.ConnectionFailure => UnitOfWorkOutcome.Failure.ConnectionLost,
        PostgresErrorCodes.ConnectionDoesNotExist => UnitOfWorkOutcome.Failure.ConnectionLost,
        PostgresErrorCodes.ConnectionException => UnitOfWorkOutcome.Failure.ConnectionFailed,
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

    /// <summary>Fallback for DbUpdateException when no PostgresException is in the chain.</summary>
    private static Error TranslateDbUpdate(DbUpdateException ex)
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